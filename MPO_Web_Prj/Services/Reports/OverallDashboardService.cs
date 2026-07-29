using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class OverallDashboardService : IOverallDashboardService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 0);
    private const int TopWorstLimit = 10;

    private readonly AppDbContext dbContext;
    private readonly IBoardCountChartService boardCountChartService;

    public OverallDashboardService(AppDbContext dbContext, IBoardCountChartService boardCountChartService)
    {
        this.dbContext = dbContext;
        this.boardCountChartService = boardCountChartService;
    }

    public async Task<OverallDashboardViewModel> GetDashboardAsync(BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        var boardProduced = await boardCountChartService.GetChartAsync(filter, cancellationToken);

        if (!boardProduced.Filter.IsApplied)
        {
            return new OverallDashboardViewModel
            {
                BoardProduced = boardProduced,
                ErrorMessage = boardProduced.ErrorMessage
            };
        }

        try
        {
            var selectedLines = GetSelectedLines(boardProduced.Filter);
            var startDateTime = boardProduced.Filter.StartDate?.ToDateTime(boardProduced.Filter.StartTime ?? StartOfDay);
            var endDateTime = boardProduced.Filter.EndDate?.ToDateTime(boardProduced.Filter.EndTime ?? EndOfDay).AddHours(1);

            var productionQuery = ApplyProductionFilters(
                dbContext.production_reports.AsNoTracking().Include(report => report.machine),
                selectedLines,
                startDateTime,
                endDateTime);

            var errorStopTime = await BuildErrorStopTimeAsync(productionQuery, cancellationToken);
            var topWorstFeeders = await BuildTopWorstFeedersAsync(selectedLines, startDateTime, endDateTime, cancellationToken);
            var topWorstNozzles = await BuildTopWorstNozzlesAsync(selectedLines, startDateTime, endDateTime, cancellationToken);

            return new OverallDashboardViewModel
            {
                BoardProduced = boardProduced,
                ErrorStopTime = errorStopTime,
                TopWorstFeeders = topWorstFeeders,
                TopWorstNozzles = topWorstNozzles,
                ErrorMessage = boardProduced.ErrorMessage
            };
        }
        catch (NpgsqlException ex)
        {
            return CreateDatabaseErrorViewModel(boardProduced, ex);
        }
        catch (TimeoutException ex)
        {
            return CreateDatabaseErrorViewModel(boardProduced, ex);
        }
        catch (InvalidOperationException ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(boardProduced, ex);
        }
    }

    private static IQueryable<MPO_Web_Prj.Models.production_report> ApplyProductionFilters(
        IQueryable<MPO_Web_Prj.Models.production_report> query,
        IReadOnlyList<string> selectedLines,
        DateTime? startDateTime,
        DateTime? endDateTime)
    {
        if (selectedLines.Count > 0)
        {
            query = query.Where(report => report.machine != null
                && report.machine.line != null
                && selectedLines.Contains(report.machine.line));
        }

        if (startDateTime.HasValue)
        {
            query = query.Where(report => report.report_date >= startDateTime.Value);
        }

        if (endDateTime.HasValue)
        {
            query = query.Where(report => report.report_date < endDateTime.Value);
        }

        return query;
    }

    private static async Task<IReadOnlyList<OverallErrorStopSlice>> BuildErrorStopTimeAsync(
        IQueryable<MPO_Web_Prj.Models.production_report> query,
        CancellationToken cancellationToken)
    {
        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TimeFwait = group.Sum(report => report.time_fwait ?? 0),
                TimeRwait = group.Sum(report => report.time_rwait ?? 0),
                TimeScstop = group.Sum(report => report.time_scstop ?? 0),
                TimePwait = group.Sum(report => report.time_pwait ?? 0),
                TimeCperr = group.Sum(report => report.time_cperr ?? 0),
                TimeCrerr = group.Sum(report => report.time_crerr ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (totals == null)
        {
            return [];
        }

        return new List<OverallErrorStopSlice>
        {
            new() { Label = "Latency for previous wait", Value = totals.TimeFwait },
            new() { Label = "Latency for next wait", Value = totals.TimeRwait },
            new() { Label = "Cycle stop", Value = totals.TimeScstop },
            new() { Label = "Latency for component supply", Value = totals.TimePwait },
            new() { Label = "Chip pickup error", Value = totals.TimeCperr },
            new() { Label = "Chip recog error", Value = totals.TimeCrerr }
        };
    }

    private async Task<IReadOnlyList<OverallWorstFeederRow>> BuildTopWorstFeedersAsync(
        IReadOnlyList<string> selectedLines,
        DateTime? startDateTime,
        DateTime? endDateTime,
        CancellationToken cancellationToken)
    {
        var query = dbContext.feeder_logs
            .AsNoTracking()
            .Where(log => log.report != null)
            .AsQueryable();

        query = ApplyFeederReportFilters(query, selectedLines, startDateTime, endDateTime);

        var groupedRows = await query
            .GroupBy(log => new
            {
                FeederId = log.blk_serial ?? string.Empty,
                FeederAdd = log.f_add ?? string.Empty,
                FeederSubAdd = log.fs_add,
                PartName = log.part_name ?? string.Empty
            })
            .Select(group => new
            {
                group.Key.FeederId,
                group.Key.FeederAdd,
                group.Key.FeederSubAdd,
                group.Key.PartName,
                TotalPickup = group.Sum(log => (long)(log.f_pickup_qty ?? 0)),
                TotalPlacement = group.Sum(log => (long)(log.f_mount_qty ?? 0))
            })
            .ToListAsync(cancellationToken);

        return groupedRows
            .Select(row => new OverallWorstFeederRow
            {
                FeederId = row.FeederId,
                FeederSlot = FormatSlot(row.FeederAdd, row.FeederSubAdd),
                PartName = row.PartName,
                TotalPickup = row.TotalPickup,
                TotalPlacement = row.TotalPlacement,
                Ppm = CalculatePpm(row.TotalPickup, row.TotalPlacement)
            })
            .Where(row => row.TotalPickup > 0)
            .OrderByDescending(row => row.Ppm)
            .ThenByDescending(row => row.TotalPickup)
            .ThenBy(row => row.FeederId)
            .Take(TopWorstLimit)
            .ToList();
    }

    private async Task<IReadOnlyList<OverallWorstNozzleRow>> BuildTopWorstNozzlesAsync(
        IReadOnlyList<string> selectedLines,
        DateTime? startDateTime,
        DateTime? endDateTime,
        CancellationToken cancellationToken)
    {
        var query = dbContext.nozzle_logs
            .AsNoTracking()
            .Where(log => log.report != null)
            .AsQueryable();

        query = ApplyNozzleReportFilters(query, selectedLines, startDateTime, endDateTime);

        var groupedRows = await query
            .GroupBy(log => new
            {
                NozzleSlot = log.nc_add ?? string.Empty,
                HeadNum = log.head_num,
                NozzleHeadAdd = log.nh_add,
                PartName = log.nozzle_name ?? string.Empty
            })
            .Select(group => new
            {
                group.Key.NozzleSlot,
                group.Key.HeadNum,
                group.Key.NozzleHeadAdd,
                group.Key.PartName,
                TotalPickup = group.Sum(log => (long)(log.n_pickup_qty ?? 0)),
                TotalPlacement = group.Sum(log => (long)(log.n_mount_qty ?? 0))
            })
            .ToListAsync(cancellationToken);

        return groupedRows
            .Select(row => new OverallWorstNozzleRow
            {
                NozzleSlot = row.NozzleSlot,
                Head = FormatSlot(row.HeadNum?.ToString() ?? string.Empty, row.NozzleHeadAdd),
                PartName = row.PartName,
                TotalPickup = row.TotalPickup,
                TotalPlacement = row.TotalPlacement,
                Ppm = CalculatePpm(row.TotalPickup, row.TotalPlacement)
            })
            .Where(row => row.TotalPickup > 0)
            .OrderByDescending(row => row.Ppm)
            .ThenByDescending(row => row.TotalPickup)
            .ThenBy(row => row.NozzleSlot)
            .Take(TopWorstLimit)
            .ToList();
    }

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyFeederReportFilters(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        IReadOnlyList<string> selectedLines,
        DateTime? startDateTime,
        DateTime? endDateTime)
    {
        if (selectedLines.Count > 0)
        {
            query = query.Where(log => log.report!.machine != null
                && log.report.machine.line != null
                && selectedLines.Contains(log.report.machine.line));
        }

        if (startDateTime.HasValue)
        {
            query = query.Where(log => log.report!.report_date >= startDateTime.Value);
        }

        if (endDateTime.HasValue)
        {
            query = query.Where(log => log.report!.report_date < endDateTime.Value);
        }

        return query;
    }

    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyNozzleReportFilters(
        IQueryable<MPO_Web_Prj.Models.nozzle_log> query,
        IReadOnlyList<string> selectedLines,
        DateTime? startDateTime,
        DateTime? endDateTime)
    {
        if (selectedLines.Count > 0)
        {
            query = query.Where(log => log.report!.machine != null
                && log.report.machine.line != null
                && selectedLines.Contains(log.report.machine.line));
        }

        if (startDateTime.HasValue)
        {
            query = query.Where(log => log.report!.report_date >= startDateTime.Value);
        }

        if (endDateTime.HasValue)
        {
            query = query.Where(log => log.report!.report_date < endDateTime.Value);
        }

        return query;
    }

    private static IReadOnlyList<string> GetSelectedLines(BoardCountChartFilter filter)
    {
        return new[] { filter.Line1, filter.Line2, filter.Line3, filter.Line4 }
            .Take(Math.Clamp(filter.Type, 1, 4))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line!)
            .ToList();
    }

    private static string FormatSlot(string slot, short? subSlot)
    {
        return string.IsNullOrWhiteSpace(slot) && !subSlot.HasValue
            ? string.Empty
            : $"{slot}_{subSlot?.ToString() ?? string.Empty}";
    }

    private static decimal CalculatePpm(long totalPickup, long totalPlacement)
    {
        if (totalPickup <= 0)
        {
            return 0;
        }

        return decimal.Round(1_000_000m * (totalPickup - totalPlacement) / totalPickup, 2);
    }

    private static OverallDashboardViewModel CreateDatabaseErrorViewModel(BoardCountChartViewModel boardProduced, Exception exception)
    {
        return new OverallDashboardViewModel
        {
            BoardProduced = boardProduced,
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
