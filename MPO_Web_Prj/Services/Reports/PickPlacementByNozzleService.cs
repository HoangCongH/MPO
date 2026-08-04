using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class PickPlacementByNozzleService : IPickPlacementByNozzleService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public PickPlacementByNozzleService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PickPlacementByNozzleViewModel> GetReportAsync(PickPlacementByNozzleFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);

        try
        {
            var lineOptions = await BuildOptionsAsync(
                dbContext.master_machines
                    .AsNoTracking()
                    .Where(machine => machine.line != null && machine.line != string.Empty)
                    .Select(machine => machine.line!),
                cancellationToken);

            var machineOptionQuery = BuildMachineOptionQuery(filter);
            var machineNameOptions = await BuildOptionsAsync(
                machineOptionQuery
                    .Where(machine => machine.machine_name != null && machine.machine_name != string.Empty)
                    .Select(machine => machine.machine_name!),
                cancellationToken);

            var stageOptions = await BuildOptionsAsync(
                machineOptionQuery
                    .Where(machine => machine.stage != null)
                    .Select(machine => machine.stage!.Value.ToString()),
                cancellationToken);

            if (!filter.IsApplied)
            {
                return new PickPlacementByNozzleViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    MachineNameOptions = machineNameOptions,
                    StageOptions = stageOptions,
                    PartOptions = DefaultOptions(),
                    NozzleSlotOptions = DefaultOptions(),
                    Pagination = ReportPaging.Create(filter.Page, 0),
                    Rows = []
                };
            }

            var latestReportDate = await dbContext.nozzle_logs
                .AsNoTracking()
                .Where(log => log.report != null && log.report.report_date != null)
                .OrderByDescending(log => log.report!.report_date)
                .Select(log => log.report!.report_date)
                .FirstOrDefaultAsync(cancellationToken);

            SetDefaultDateRange(filter, latestReportDate);

            var partOptions = await BuildOptionsAsync(
                ApplyMachineFilters(dbContext.nozzle_logs.AsNoTracking(), filter)
                    .Where(log => log.nozzle_name != null && log.nozzle_name != string.Empty)
                    .Select(log => log.nozzle_name!),
                cancellationToken);

            var nozzleSlotOptions = await BuildOptionsAsync(
                ApplyMachineFilters(dbContext.nozzle_logs.AsNoTracking(), filter)
                    .Where(log => log.nh_add != null)
                    .Select(log => log.nh_add!.Value.ToString()),
                cancellationToken);

            var query = dbContext.nozzle_logs
                .AsNoTracking()
                .Where(log => log.report != null)
                .AsQueryable();

            query = ApplyMachineFilters(query, filter);

            if (!string.IsNullOrWhiteSpace(filter.PartName))
            {
                query = query.Where(log => log.nozzle_name == filter.PartName);
            }

            if (!string.IsNullOrWhiteSpace(filter.NozzleSlot)
                && short.TryParse(filter.NozzleSlot, out var nozzleSlot))
            {
                query = query.Where(log => log.nh_add == nozzleSlot);
            }

            query = ApplyDateFilter(query, filter);

            var groupedQuery = query.GroupBy(log => new
            {
                PartName = log.nozzle_name,
                LineName = log.report!.machine != null ? log.report.machine.line : null,
                MachineName = log.report!.machine != null ? log.report.machine.machine_name : null,
                Stage = log.report!.machine != null ? log.report.machine.stage : null,
                NozzleSlot = log.nh_add,
                NozzleChangerSlot = log.nc_add
            });

            var totalRecords = await groupedQuery.CountAsync(cancellationToken);
            var pagination = ReportPaging.Create(filter.Page, totalRecords);
            filter.Page = pagination.Page;

            var reportRows = await groupedQuery
                .OrderByDescending(g => g.Max(log => log.report!.report_date))
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .Select(g => new
                {
                    PartName = g.Key.PartName ?? string.Empty,
                    LineName = g.Key.LineName ?? string.Empty,
                    MachineName = g.Key.MachineName ?? string.Empty,
                    Stage = g.Key.Stage != null ? g.Key.Stage.ToString() : string.Empty,
                    NozzleSlot = g.Key.NozzleSlot,
                    NozzleChangerSlot = g.Key.NozzleChangerSlot ?? string.Empty,
                    PickupCount = g.Sum(log => log.n_pickup_qty ?? 0),
                    PlacementCount = g.Sum(log => log.n_mount_qty ?? 0),
                    PickupMiss = g.Sum(log => log.n_p_miss_qty ?? 0),
                    RecogMiss = g.Sum(log => log.n_r_miss_qty ?? 0),
                    HeightMiss = g.Sum(log => log.n_h_miss_qty ?? 0),
                    DropMiss = g.Sum(log => log.n_d_miss_qty ?? 0),
                    MountMiss = g.Sum(log => log.n_m_miss_qty ?? 0),
                    TransferMiss = g.Sum(log => log.n_trs_miss_qty ?? 0)
                })
                .ToListAsync(cancellationToken);

            return new PickPlacementByNozzleViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                MachineNameOptions = machineNameOptions,
                StageOptions = stageOptions,
                PartOptions = partOptions,
                NozzleSlotOptions = nozzleSlotOptions,
                Pagination = pagination,
                Rows = reportRows.Select(row => new PickPlacementByNozzleRow
                {
                    PartName = row.PartName,
                    LineName = row.LineName,
                    MachineName = row.MachineName,
                    Stage = row.Stage,
                    NozzleSlot = row.NozzleSlot?.ToString() ?? string.Empty,
                    NozzleChangerSlot = row.NozzleChangerSlot,
                    PickupCount = row.PickupCount,
                    PlacementCount = row.PlacementCount,
                    PickupMiss = row.PickupMiss,
                    RecogMiss = row.RecogMiss,
                    HeightMiss = row.HeightMiss,
                    DropMiss = row.DropMiss,
                    MountMiss = row.MountMiss,
                    TransferMiss = row.TransferMiss,
                    ScrapRatio = CalculateScrapRatio(row.PickupCount, row.PlacementCount)
                }).ToList()
            };
        }
        catch (NpgsqlException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (TimeoutException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (InvalidOperationException ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    private IQueryable<MPO_Web_Prj.Models.master_machine> BuildMachineOptionQuery(PickPlacementByNozzleFilter filter)
    {
        var query = dbContext.master_machines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.LineName))
        {
            query = query.Where(machine => machine.line == filter.LineName);
        }

        return query;
    }

    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyMachineFilters(
        IQueryable<MPO_Web_Prj.Models.nozzle_log> query,
        PickPlacementByNozzleFilter filter)
    {
        query = ApplyLineFilter(query, filter.LineName);

        if (!string.IsNullOrWhiteSpace(filter.MachineName))
        {
            query = query.Where(log => log.report != null
                && log.report.machine != null
                && log.report.machine.machine_name == filter.MachineName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Stage)
            && short.TryParse(filter.Stage, out var stage))
        {
            query = query.Where(log => log.report != null
                && log.report.machine != null
                && log.report.machine.stage == stage);
        }

        return query;
    }

    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyLineFilter(
        IQueryable<MPO_Web_Prj.Models.nozzle_log> query,
        string? lineName)
    {
        return string.IsNullOrWhiteSpace(lineName)
            ? query
            : query.Where(log => log.report != null
                && log.report.machine != null
                && log.report.machine.line == lineName);
    }

    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyDateFilter(
        IQueryable<MPO_Web_Prj.Models.nozzle_log> query,
        PickPlacementByNozzleFilter filter)
    {
        if (filter.StartDate.HasValue)
        {
            var startDateTime = filter.StartDate.Value.ToDateTime(filter.StartTime ?? StartOfDay);
            query = query.Where(log => log.report!.report_date >= startDateTime);
        }

        if (filter.EndDate.HasValue)
        {
            if (filter.EndTime.HasValue && filter.EndTime.Value != EndOfDay)
            {
                var endDateTime = filter.EndDate.Value.ToDateTime(filter.EndTime.Value);
                query = query.Where(log => log.report!.report_date <= endDateTime);
            }
            else
            {
                var nextDateTime = filter.EndDate.Value.AddDays(1).ToDateTime(StartOfDay);
                query = query.Where(log => log.report!.report_date < nextDateTime);
            }
        }

        return query;
    }

    private static async Task<IReadOnlyList<ReportSelectOption>> BuildOptionsAsync(
        IQueryable<string> values,
        CancellationToken cancellationToken)
    {
        var options = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        options.AddRange(await values
            .Distinct()
            .OrderBy(value => value)
            .Select(value => new ReportSelectOption
            {
                Value = value,
                Text = value
            })
            .ToListAsync(cancellationToken));

        return options;
    }

    private static IReadOnlyList<ReportSelectOption> DefaultOptions()
    {
        return new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };
    }

    private static decimal CalculateScrapRatio(int pickupCount, int placementCount)
    {
        if (pickupCount <= 0)
        {
            return 0;
        }

        return decimal.Round(1_000_000m * (1 - ((decimal)placementCount / pickupCount)), 2);
    }

    private static void NormalizeFilter(PickPlacementByNozzleFilter filter)
    {
        filter.LineName = Normalize(filter.LineName);
        filter.MachineName = Normalize(filter.MachineName);
        filter.Stage = Normalize(filter.Stage);
        filter.PartName = Normalize(filter.PartName);
        filter.NozzleSlot = Normalize(filter.NozzleSlot);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void SetDefaultDateRange(PickPlacementByNozzleFilter filter, DateTime? latestReportDate)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue || !latestReportDate.HasValue)
        {
            return;
        }

        var latestDate = DateOnly.FromDateTime(latestReportDate.Value);
        filter.StartDate = latestDate;
        filter.StartTime = StartOfDay;
        filter.EndDate = latestDate;
        filter.EndTime = EndOfDay;
    }

    private static PickPlacementByNozzleViewModel CreateDatabaseErrorViewModel(PickPlacementByNozzleFilter filter, Exception exception)
    {
        return new PickPlacementByNozzleViewModel
        {
            Filter = filter,
            Pagination = ReportPaging.Create(filter.Page, 0),
            ErrorMessage = $"Cannot connect to PostgreSQL database. Please check the DB server/IP, network/VPN, port 5432, database name, username and password. Detail: {exception.Message}"
        };
    }

    private static bool IsDatabaseConnectionFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is NpgsqlException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
