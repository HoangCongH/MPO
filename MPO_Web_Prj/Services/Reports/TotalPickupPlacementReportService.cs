using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class TotalPickupPlacementReportService : ITotalPickupPlacementReportService
{
    private readonly AppDbContext dbContext;

    public TotalPickupPlacementReportService(AppDbContext dbContext) => this.dbContext = dbContext;

    public async Task<TotalPickupPlacementReportViewModel> GetReportAsync(TotalPickupPlacementReportFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        try
        {
            var lineOptions = await BuildLineOptionsAsync(cancellationToken);
            if (!filter.IsApplied)
            {
                return new TotalPickupPlacementReportViewModel { Filter = filter, LineOptions = lineOptions, Pagination = ReportPaging.Create(1, 0), Rows = [] };
            }

            var batch = await GetBatchAsync(filter, 0, filter.ExportAll ? int.MaxValue : ReportQueryParameters.BatchSize, cancellationToken);
            return new TotalPickupPlacementReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                Pagination = new ReportPagination
                {
                    Page = 1,
                    PageSize = filter.ExportAll ? Math.Max(batch.TotalRecords, 1) : ReportQueryParameters.BatchSize,
                    TotalRecords = batch.TotalRecords
                },
                Rows = batch.Rows
            };
        }
        catch (Exception ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    public async Task<ReportBatch<TotalPickupPlacementReportRow>> GetBatchAsync(
        TotalPickupPlacementReportFilter filter,
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
                    COALESCE(SUM(pr.count_pickup), 0)::bigint AS "TotalPickup",
                    COALESCE(SUM(pr.count_mount), 0)::bigint AS "TotalPlacement",
                    MAX(pr.report_date) AS "LatestReportDate"
                FROM production_reports pr
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
                  AND (@lineName IS NULL OR mm.line = @lineName)
                GROUP BY mm.line
            )
            SELECT "LineName", "TotalPickup", "TotalPlacement", COUNT(*) OVER() AS "TotalRecords"
            FROM grouped
            ORDER BY "LatestReportDate" DESC, "LineName"
            LIMIT @take OFFSET @offset
            """;

        var sqlRows = await dbContext.Database.SqlQueryRaw<TotalPickupPlacementReportSqlRow>(sql,
            ReportQueryParameters.Timestamp("startAt", startAt),
            ReportQueryParameters.Timestamp("endAt", endAt),
            ReportQueryParameters.Text("lineName", filter.LineName),
            ReportQueryParameters.Integer("take", Math.Clamp(take, 1, int.MaxValue)),
            ReportQueryParameters.Integer("offset", Math.Max(offset, 0)))
            .ToListAsync(cancellationToken);
        var rows = sqlRows.Select(row => new TotalPickupPlacementReportRow
        {
            LineName = row.LineName,
            TotalPickup = row.TotalPickup,
            TotalPlacement = row.TotalPlacement,
            Ppm = CalculatePpm(row.TotalPickup, row.TotalPlacement)
        }).ToList();
        return ReportQueryParameters.Batch(rows, sqlRows.FirstOrDefault()?.TotalRecords ?? 0, offset);
    }

    private async Task<IReadOnlyList<ReportSelectOption>> BuildLineOptionsAsync(CancellationToken cancellationToken)
    {
        var values = await dbContext.master_machines.AsNoTracking()
            .Where(machine => machine.line != null && machine.line != string.Empty)
            .Select(machine => machine.line!).Distinct().OrderBy(value => value).ToListAsync(cancellationToken);
        return ReportQueryParameters.WithFixedOptions(values.Select(value => new ReportSelectOption { Value = value, Text = value }).ToList(), null);
    }

    private static decimal CalculatePpm(long pickup, long placement) => pickup <= 0 ? 0 : decimal.Round(1_000_000m * (pickup - placement) / pickup, 2);
    private static void NormalizeFilter(TotalPickupPlacementReportFilter filter) => filter.LineName = string.IsNullOrWhiteSpace(filter.LineName) ? null : filter.LineName.Trim();
    private static TotalPickupPlacementReportViewModel CreateDatabaseErrorViewModel(TotalPickupPlacementReportFilter filter, Exception exception) => new()
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
