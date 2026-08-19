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
            WITH normalized AS (
                SELECT
                    CASE
                        WHEN regexp_replace(lower(btrim(COALESCE(mm.line, ''))), '[^a-z0-9]', '', 'g')
                             IN ('line1', '1', 'line20', '20')
                            THEN '20'
                        ELSE btrim(COALESCE(mm.line, ''))
                    END AS "LineName",
                    pr.count_cperr,
                    pr.time_cperr,
                    pr.count_crerr,
                    pr.time_crerr,
                    pr.count_scestop,
                    pr.time_scestop,
                    pr.count_trbl,
                    pr.time_trbl,
                    pr.count_pwait,
                    pr.time_pwait,
                    pr.report_date
                FROM production_reports pr
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
            ),
            grouped AS (
                SELECT
                    "LineName",
                    COALESCE(SUM(count_cperr), 0)::bigint AS "ChipPickupErrorCount",
                    COALESCE(SUM(time_cperr), 0) AS "ChipPickupErrorTime",
                    COALESCE(SUM(count_crerr), 0)::bigint AS "ChipRecogErrorCount",
                    COALESCE(SUM(time_crerr), 0) AS "ChipRecogErrorTime",
                    COALESCE(SUM(count_scestop), 0)::bigint AS "SingleErrorStopCount",
                    COALESCE(SUM(time_scestop), 0) AS "SingleErrorStopTime",
                    COALESCE(SUM(count_trbl), 0)::bigint AS "TroubleStopCount",
                    COALESCE(SUM(time_trbl), 0) AS "TroubleStopTime",
                    COALESCE(SUM(count_pwait), 0)::bigint AS "PartExhaustStopCount",
                    COALESCE(SUM(time_pwait), 0) AS "PartExhaustStopTime",
                    MAX(report_date) AS "LatestReportDate"
                FROM normalized
                WHERE (@lineName IS NULL OR "LineName" = @lineName)
                GROUP BY "LineName"
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
        var canonicalValues = values
            .Select(CanonicalizeLine)
            .Where(value => value != null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => int.TryParse(value, out var lineNumber) ? lineNumber : int.MaxValue)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Select(value => new ReportSelectOption { Value = value, Text = value })
            .ToList();
        return ReportQueryParameters.WithFixedOptions(canonicalValues, null);
    }

    private static ReportPagination CreatePagination(int totalRecords, bool exportAll) => new()
    {
        Page = 1,
        PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportQueryParameters.BatchSize,
        TotalRecords = totalRecords
    };

    private static void NormalizeFilter(DowntimeReportFilter filter) =>
        filter.LineName = CanonicalizeLine(filter.LineName);

    private static string? CanonicalizeLine(string? lineName)
    {
        if (string.IsNullOrWhiteSpace(lineName)) return null;
        var trimmed = lineName.Trim();
        var normalized = new string(trimmed.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return normalized is "1" or "line1" or "20" or "line20" ? "20" : trimmed;
    }

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
