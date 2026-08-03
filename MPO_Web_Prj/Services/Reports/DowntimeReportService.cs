using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class DowntimeReportService : IDowntimeReportService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 59, 59);

    private readonly AppDbContext dbContext;

    public DowntimeReportService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DowntimeReportViewModel> GetReportAsync(DowntimeReportFilter filter, CancellationToken cancellationToken)
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

            if (!filter.IsApplied)
            {
                return new DowntimeReportViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    Pagination = ReportPaging.Create(filter.Page, 0),
                    Rows = []
                };
            }

            var latestReportDate = await dbContext.production_reports
                .AsNoTracking()
                .Where(report => report.report_date != null)
                .OrderByDescending(report => report.report_date)
                .Select(report => report.report_date)
                .FirstOrDefaultAsync(cancellationToken);

            SetDefaultDateRange(filter, latestReportDate);

            if (!lineOptions.Any(option => option.Value == filter.LineName))
            {
                filter.LineName = null;
            }

            var query = dbContext.production_reports
                .AsNoTracking()
                .Include(report => report.machine)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.LineName))
            {
                query = query.Where(report => report.machine != null
                    && report.machine.line == filter.LineName);
            }

            if (filter.StartDate.HasValue)
            {
                var startDateTime = filter.StartDate.Value.ToDateTime(filter.StartTime ?? StartOfDay);
                query = query.Where(report => report.report_date >= startDateTime);
            }

            if (filter.EndDate.HasValue)
            {
                if (filter.EndTime.HasValue && filter.EndTime.Value != EndOfDay)
                {
                    var endDateTime = filter.EndDate.Value.ToDateTime(filter.EndTime.Value);
                    query = query.Where(report => report.report_date <= endDateTime);
                }
                else
                {
                    var nextDateTime = filter.EndDate.Value.AddDays(1).ToDateTime(StartOfDay);
                    query = query.Where(report => report.report_date < nextDateTime);
                }
            }

            var rows = await query
                .GroupBy(report => report.machine != null && report.machine.line != null
                    ? report.machine.line
                    : string.Empty)
                .Select(group => new DowntimeReportRow
                {
                    LineName = group.Key,
                    ChipPickupErrorCount = group.Sum(report => (long)(report.count_cperr ?? 0)),
                    ChipPickupErrorTime = group.Sum(report => report.time_cperr ?? 0),
                    ChipRecogErrorCount = group.Sum(report => (long)(report.count_crerr ?? 0)),
                    ChipRecogErrorTime = group.Sum(report => report.time_crerr ?? 0),
                    SingleErrorStopCount = group.Sum(report => (long)(report.count_scestop ?? 0)),
                    SingleErrorStopTime = group.Sum(report => report.time_scestop ?? 0),
                    TroubleStopCount = group.Sum(report => (long)(report.count_trbl ?? 0)),
                    TroubleStopTime = group.Sum(report => report.time_trbl ?? 0),
                    PartExhaustStopCount = group.Sum(report => (long)(report.count_pwait ?? 0)),
                    PartExhaustStopTime = group.Sum(report => report.time_pwait ?? 0),
                    LatestReportDate = group.Max(report => report.report_date)
                })
                .OrderByDescending(row => row.LatestReportDate)
                .ThenBy(row => row.LineName)
                .ToListAsync(cancellationToken);

            var pagination = ReportPaging.Create(filter.Page, rows.Count);
            filter.Page = pagination.Page;
            rows = rows
                .Skip(pagination.Skip)
                .Take(pagination.PageSize)
                .ToList();

            return new DowntimeReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                Pagination = pagination,
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

    private static DowntimeReportViewModel CreateDatabaseErrorViewModel(DowntimeReportFilter filter, Exception exception)
    {
        return new DowntimeReportViewModel
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

    private static void NormalizeFilter(DowntimeReportFilter filter)
    {
        filter.LineName = string.IsNullOrWhiteSpace(filter.LineName)
            ? null
            : filter.LineName.Trim();
    }

    private static void SetDefaultDateRange(DowntimeReportFilter filter, DateTime? latestReportDate)
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
