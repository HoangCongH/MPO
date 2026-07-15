using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class PickPlacementByPartService : IPickPlacementByPartService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public PickPlacementByPartService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PickPlacementByPartViewModel> GetReportAsync(PickPlacementByPartFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);

        try
        {
        var lineOptions = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        lineOptions.AddRange(await dbContext.master_machines
            .AsNoTracking()
            .Where(machine => machine.line != null && machine.line != string.Empty)
            .Select(machine => machine.line!)
            .Distinct()
            .OrderBy(lineName => lineName)
            .Select(lineName => new ReportSelectOption
            {
                Value = lineName,
                Text = lineName
            })
            .ToListAsync(cancellationToken));

        var partOptions = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        if (!filter.IsApplied)
        {
            return new PickPlacementByPartViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                PartOptions = partOptions,
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

        var partOptionQuery = dbContext.feeder_logs
            .AsNoTracking()
            .Where(log => log.part_name != null && log.part_name != string.Empty);

        if (!string.IsNullOrWhiteSpace(filter.LineName))
        {
            partOptionQuery = partOptionQuery.Where(log => log.report != null
                && log.report.machine != null
                && log.report.machine.line == filter.LineName);
        }

        partOptions.AddRange(await partOptionQuery
            .Select(log => log.part_name!)
            .Distinct()
            .OrderBy(partName => partName)
            .Select(partName => new ReportSelectOption
            {
                Value = partName,
                Text = partName
            })
            .ToListAsync(cancellationToken));

        var query = dbContext.feeder_logs
            .AsNoTracking()
            .Where(log => log.report != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.LineName))
        {
            query = query.Where(log => log.report!.machine != null
                && log.report.machine.line == filter.LineName);
        }

        if (!string.IsNullOrWhiteSpace(filter.PartName))
        {
            query = query.Where(log => log.part_name == filter.PartName);
        }

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

        var groupedRows = await query
            .GroupBy(log => new
            {
                LineName = log.report!.machine != null && log.report.machine.line != null
                    ? log.report.machine.line
                    : string.Empty,
                PartName = log.part_name ?? string.Empty
            })
            .Select(group => new
            {
                group.Key.LineName,
                group.Key.PartName,
                PickupCount = group.Sum(log => log.f_pickup_qty ?? 0),
                PlacementCount = group.Sum(log => log.f_mount_qty ?? 0),
                PickupMiss = group.Sum(log => log.f_p_miss_qty ?? 0),
                RecogMiss = group.Sum(log => log.f_r_miss_qty ?? 0),
                HeightMiss = group.Sum(log => log.f_h_miss_qty ?? 0),
                DropMiss = group.Sum(log => log.f_d_miss_qty ?? 0),
                MountMiss = group.Sum(log => log.f_m_miss_qty ?? 0),
                TransferMiss = group.Sum(log => log.f_trs_miss_qty ?? 0)
            })
            .OrderByDescending(row => row.PickupCount)
            .ThenBy(row => row.LineName)
            .ThenBy(row => row.PartName)
            .ToListAsync(cancellationToken);

        var rows = groupedRows
            .Select(row => new PickPlacementByPartRow
            {
                LineName = row.LineName,
                PartName = row.PartName,
                PickupCount = row.PickupCount,
                PlacementCount = row.PlacementCount,
                PickupMiss = row.PickupMiss,
                RecogMiss = row.RecogMiss,
                HeightMiss = row.HeightMiss,
                DropMiss = row.DropMiss,
                MountMiss = row.MountMiss,
                TransferMiss = row.TransferMiss,
                ScrapRatio = CalculateScrapRatio(row.PickupCount, row.PlacementCount)
            })
            .ToList();

        return new PickPlacementByPartViewModel
        {
            Filter = filter,
            LineOptions = lineOptions,
            PartOptions = partOptions,
            Rows = rows
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

    private static PickPlacementByPartViewModel CreateDatabaseErrorViewModel(PickPlacementByPartFilter filter, Exception exception)
    {
        return new PickPlacementByPartViewModel
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

    private static decimal CalculateScrapRatio(int pickupCount, int placementCount)
    {
        if (pickupCount <= 0)
        {
            return 0;
        }

        return decimal.Round(1_000_000m * (1 - ((decimal)placementCount / pickupCount)), 2);
    }

    private static void NormalizeFilter(PickPlacementByPartFilter filter)
    {
        filter.LineName = string.IsNullOrWhiteSpace(filter.LineName)
            ? null
            : filter.LineName.Trim();

        filter.PartName = string.IsNullOrWhiteSpace(filter.PartName)
            ? null
            : filter.PartName.Trim();
    }

    private static void SetDefaultDateRange(PickPlacementByPartFilter filter, DateTime? latestReportDate)
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
}
