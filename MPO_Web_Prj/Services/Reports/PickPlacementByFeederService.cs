using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class PickPlacementByFeederService : IPickPlacementByFeederService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public PickPlacementByFeederService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PickPlacementByFeederViewModel> GetReportAsync(PickPlacementByFeederFilter filter, CancellationToken cancellationToken)
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
                return new PickPlacementByFeederViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    MachineNameOptions = machineNameOptions,
                    StageOptions = stageOptions,
                    PartOptions = DefaultOptions(),
                    FeederIdOptions = DefaultOptions(),
                    FeederSlotOptions = DefaultOptions(),
                    Rows = []
                };
            }

            var latestReportDate = await dbContext.feeder_logs
                .AsNoTracking()
                .Where(log => log.report != null && log.report.report_date != null)
                .OrderByDescending(log => log.report!.report_date)
                .Select(log => log.report!.report_date)
                .FirstOrDefaultAsync(cancellationToken);

            SetDefaultDateRange(filter, latestReportDate);

            var partOptions = await BuildOptionsAsync(
                ApplyMachineFilters(dbContext.feeder_logs.AsNoTracking(), filter)
                    .Where(log => log.part_name != null && log.part_name != string.Empty)
                    .Select(log => log.part_name!),
                cancellationToken
                );
            //var partOptions = await BuildOptionsAsync(
            //    ApplyLineFilter(dbContext.feeder_logs.AsNoTracking(), filter.LineName)
            //        .Where(log => log.part_name != null && log.part_name != string.Empty)
            //        .Select(log => log.part_name!),
            //    cancellationToken);

            var feederIdOptions = await BuildOptionsAsync(
                ApplyLineFilter(dbContext.feeder_logs.AsNoTracking(), filter.LineName)
                    .Where(log => log.blk_serial != null && log.blk_serial != string.Empty)
                    .Select(log => log.blk_serial!),
                cancellationToken);

            var feederSlotOptions = await BuildFeederSlotOptionsAsync(
                ApplyLineFilter(dbContext.feeder_logs.AsNoTracking(), filter.LineName)
                    .Where(log => log.f_add != null && log.f_add != string.Empty),
                cancellationToken);

            var query = dbContext.feeder_logs
                .AsNoTracking()
                .Where(log => log.report != null)
                .AsQueryable();

            query = ApplyMachineFilters(query, filter);

            if (!string.IsNullOrWhiteSpace(filter.PartName))
            {
                query = query.Where(log => log.part_name == filter.PartName);
            }

            if (!string.IsNullOrWhiteSpace(filter.FeederId))
            {
                query = query.Where(log => log.blk_serial == filter.FeederId);
            }

            if (!string.IsNullOrWhiteSpace(filter.FeederSlot))
            {
                query = query.Where(log => log.f_add == filter.FeederSlot);
            }

            query = ApplyDateFilter(query, filter);

            var reportRows = await query
                .Select(log => new
                {
                    PartName = log.part_name ?? string.Empty,
                    LineName = log.report!.machine != null && log.report.machine.line != null
                        ? log.report.machine.line
                        : string.Empty,
                    MachineName = log.report!.machine != null && log.report.machine.machine_name != null
                        ? log.report.machine.machine_name
                        : string.Empty,
                    Stage = log.report!.machine != null && log.report.machine.stage != null
                        ? log.report.machine.stage.Value.ToString()
                        : string.Empty,
                    FeederId = log.blk_serial ?? string.Empty,
                    FeederAdd = log.f_add ?? string.Empty,
                    FeederSubAdd = log.fs_add,
                    PickupCount = log.f_pickup_qty ?? 0,
                    PlacementCount = log.f_mount_qty ?? 0,
                    PickupMiss = log.f_p_miss_qty ?? 0,
                    RecogMiss = log.f_r_miss_qty ?? 0,
                    HeightMiss = log.f_h_miss_qty ?? 0,
                    DropMiss = log.f_d_miss_qty ?? 0,
                    MountMiss = log.f_m_miss_qty ?? 0,
                    TransferMiss = log.f_trs_miss_qty ?? 0
                })
                .OrderByDescending(row => row.PickupCount)
                .ThenBy(row => row.PartName)
                .ThenBy(row => row.LineName)
                .ToListAsync(cancellationToken);

            return new PickPlacementByFeederViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                MachineNameOptions = machineNameOptions,
                StageOptions = stageOptions,
                PartOptions = partOptions,
                FeederIdOptions = feederIdOptions,
                FeederSlotOptions = feederSlotOptions,
                Rows = reportRows.Select(row => new PickPlacementByFeederRow
                {
                    PartName = row.PartName,
                    LineName = row.LineName,
                    MachineName = row.MachineName,
                    Stage = row.Stage,
                    FeederId = row.FeederId,
                    FeederTable = GetFeederTable(row.FeederAdd),
                    FeederSlot = GetFeederSlot(row.FeederAdd),
                    Side = row.FeederSubAdd?.ToString() ?? string.Empty,
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
    private IQueryable<MPO_Web_Prj.Models.master_machine> BuildMachineOptionQuery(PickPlacementByFeederFilter filter)
    {
        var query = dbContext.master_machines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.LineName))
        {
            query = query.Where(machine => machine.line == filter.LineName);
        }

        return query;
    }

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyMachineFilters(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        PickPlacementByFeederFilter filter)
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

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyLineFilter(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        string? lineName)
    {
        return string.IsNullOrWhiteSpace(lineName)
            ? query
            : query.Where(log => log.report != null
                && log.report.machine != null
                && log.report.machine.line == lineName);
    }

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyDateFilter(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        PickPlacementByFeederFilter filter)
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

    private static async Task<IReadOnlyList<ReportSelectOption>> BuildFeederSlotOptionsAsync(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        CancellationToken cancellationToken)
    {
        var slots = await query
            .Select(log => log.f_add ?? string.Empty)
            .Distinct()
            .OrderBy(slot => slot)
            .ToListAsync(cancellationToken);

        var options = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        options.AddRange(slots
            .Select(slot => new ReportSelectOption
            {
                Value = slot,
                Text = slot
            }));

        return options;
    }

    private static string GetFeederTable(string feederAdd)
    {
        return string.IsNullOrWhiteSpace(feederAdd)
            ? string.Empty
            : feederAdd[..1];
    }

    private static string GetFeederSlot(string feederAdd)
    {
        return string.IsNullOrWhiteSpace(feederAdd) || feederAdd.Length <= 1
            ? string.Empty
            : feederAdd[1..];
    }

    private static decimal CalculateScrapRatio(int pickupCount, int placementCount)
    {
        if (pickupCount <= 0)
        {
            return 0;
        }

        return decimal.Round(1_000_000m * (1 - ((decimal)placementCount / pickupCount)), 2);
    }

    private static void NormalizeFilter(PickPlacementByFeederFilter filter)
    {
        filter.LineName = Normalize(filter.LineName);
        filter.PartName = Normalize(filter.PartName);
        filter.MachineName = Normalize(filter.MachineName);
        filter.Stage = Normalize(filter.Stage);
        filter.FeederId = Normalize(filter.FeederId);
        filter.FeederSlot = Normalize(filter.FeederSlot);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void SetDefaultDateRange(PickPlacementByFeederFilter filter, DateTime? latestReportDate)
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

    private static PickPlacementByFeederViewModel CreateDatabaseErrorViewModel(PickPlacementByFeederFilter filter, Exception exception)
    {
        return new PickPlacementByFeederViewModel
        {
            Filter = filter,
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
