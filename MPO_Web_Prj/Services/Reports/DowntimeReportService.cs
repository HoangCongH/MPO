using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class DowntimeReportService : IDowntimeReportService
{
    private readonly AppDbContext dbContext;

    public DowntimeReportService(AppDbContext dbContext) => this.dbContext = dbContext;

    public async Task<DowntimeReportViewModel> GetReportAsync(DowntimeReportFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        try
        {
            var lineOptions = await BuildLineOptionsAsync(cancellationToken);
            if (!filter.IsApplied)
            {
                return new DowntimeReportViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    Pagination = ReportPaging.Create(1, 0),
                    Rows = []
                };
            }

            var batch = await GetBatchAsync(filter, 0, filter.ExportAll ? int.MaxValue : ReportQueryParameters.BatchSize, cancellationToken);
            return new DowntimeReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                Pagination = CreatePagination(batch.TotalRecords, filter.ExportAll),
                Rows = batch.Rows
            };
        }
        catch (Exception ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    public async Task<ReportBatch<DowntimeReportRow>> GetBatchAsync(
        DowntimeReportFilter filter,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var (startAt, endAt) = ReportQueryParameters.DateRange(filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        const string sql = """
            WITH grouped AS (
                SELECT
                    COALESCE(mm.line, '') AS "LineName",
                    COALESCE(SUM(pr.count_cperr), 0)::bigint AS "ChipPickupErrorCount",
                    COALESCE(SUM(pr.time_cperr), 0) AS "ChipPickupErrorTime",
                    COALESCE(SUM(pr.count_crerr), 0)::bigint AS "ChipRecogErrorCount",
                    COALESCE(SUM(pr.time_crerr), 0) AS "ChipRecogErrorTime",
                    COALESCE(SUM(pr.count_scestop), 0)::bigint AS "SingleErrorStopCount",
                    COALESCE(SUM(pr.time_scestop), 0) AS "SingleErrorStopTime",
                    COALESCE(SUM(pr.count_trbl), 0)::bigint AS "TroubleStopCount",
                    COALESCE(SUM(pr.time_trbl), 0) AS "TroubleStopTime",
                    COALESCE(SUM(pr.count_pwait), 0)::bigint AS "PartExhaustStopCount",
                    COALESCE(SUM(pr.time_pwait), 0) AS "PartExhaustStopTime",
                    MAX(pr.report_date) AS "LatestReportDate"
                FROM production_reports pr
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
                  AND (@lineName IS NULL OR mm.line = @lineName)
                GROUP BY mm.line
            )
            SELECT
                "LineName", "ChipPickupErrorCount", "ChipPickupErrorTime", "ChipRecogErrorCount", "ChipRecogErrorTime",
                "SingleErrorStopCount", "SingleErrorStopTime", "TroubleStopCount", "TroubleStopTime",
                "PartExhaustStopCount", "PartExhaustStopTime", COUNT(*) OVER() AS "TotalRecords"
            FROM grouped
            ORDER BY "LatestReportDate" DESC, "LineName"
            LIMIT @take OFFSET @offset
            """;

        var sqlRows = await dbContext.Database.SqlQueryRaw<DowntimeReportSqlRow>(sql,
            ReportQueryParameters.Timestamp("startAt", startAt),
            ReportQueryParameters.Timestamp("endAt", endAt),
            ReportQueryParameters.Text("lineName", filter.LineName),
            ReportQueryParameters.Integer("take", Math.Clamp(take, 1, int.MaxValue)),
            ReportQueryParameters.Integer("offset", Math.Max(offset, 0)))
            .ToListAsync(cancellationToken);

        var rows = sqlRows.Select(row => new DowntimeReportRow
        {
            LineName = row.LineName,
            ChipPickupErrorCount = row.ChipPickupErrorCount,
            ChipPickupErrorTime = row.ChipPickupErrorTime,
            ChipRecogErrorCount = row.ChipRecogErrorCount,
            ChipRecogErrorTime = row.ChipRecogErrorTime,
            SingleErrorStopCount = row.SingleErrorStopCount,
            SingleErrorStopTime = row.SingleErrorStopTime,
            TroubleStopCount = row.TroubleStopCount,
            TroubleStopTime = row.TroubleStopTime,
            PartExhaustStopCount = row.PartExhaustStopCount,
            PartExhaustStopTime = row.PartExhaustStopTime
        }).ToList();
        return ReportQueryParameters.Batch(rows, sqlRows.FirstOrDefault()?.TotalRecords ?? 0, offset);
    }

    private async Task<IReadOnlyList<ReportSelectOption>> BuildLineOptionsAsync(CancellationToken cancellationToken)
    {
        var values = await dbContext.master_machines.AsNoTracking()
            .Where(machine => machine.line != null && machine.line != string.Empty)
            .Select(machine => machine.line!)
            .Distinct().OrderBy(value => value)
            .ToListAsync(cancellationToken);
        return ReportQueryParameters.WithFixedOptions(values.Select(value => new ReportSelectOption { Value = value, Text = value }).ToList(), null);
    }

    private static ReportPagination CreatePagination(int totalRecords, bool exportAll) => new()
    {
        Page = 1,
        PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportQueryParameters.BatchSize,
        TotalRecords = totalRecords
    };

    private static void NormalizeFilter(DowntimeReportFilter filter) =>
        filter.LineName = string.IsNullOrWhiteSpace(filter.LineName) ? null : filter.LineName.Trim();

    private static DowntimeReportViewModel CreateDatabaseErrorViewModel(DowntimeReportFilter filter, Exception exception) => new()
    {
        Filter = filter,
        Pagination = ReportPaging.Create(1, 0),
        ErrorMessage = $"Cannot connect to PostgreSQL database. Please check the DB server/IP, network/VPN, port 5432, database name, username and password. Detail: {exception.Message}"
    };

    private static bool IsDatabaseConnectionFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is NpgsqlException or TimeoutException) return true;
        }
        return false;
    }
}
