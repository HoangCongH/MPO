using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class ProductionReportService : IProductionReportService
{
    private readonly AppDbContext dbContext;

    public ProductionReportService(AppDbContext dbContext) => this.dbContext = dbContext;

    public async Task<ProductionReportViewModel> GetReportAsync(ProductionReportFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        try
        {
            var machines = await GetMachinesAsync(cancellationToken);
            var lineOptions = BuildLineOptions(machines);
            if (!filter.IsApplied)
            {
                return new ProductionReportViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    ModelOptions = ReportQueryParameters.SelectedOption(filter.ModelName),
                    Pagination = ReportPaging.Create(1, 0),
                    Rows = []
                };
            }

            var machineIds = GetLastMachineIdsByLine(machines, filter.LineName);
            var batch = await ExecuteBatchAsync(filter, machineIds, 0, filter.ExportAll ? int.MaxValue : ReportQueryParameters.BatchSize, cancellationToken);
            return new ProductionReportViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                ModelOptions = ReportQueryParameters.SelectedOption(filter.ModelName),
                Pagination = CreatePagination(batch.TotalRecords, filter.ExportAll),
                Rows = batch.Rows
            };
        }
        catch (Exception ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    public async Task<ReportBatch<ProductionReportRow>> GetBatchAsync(
        ProductionReportFilter filter,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var machines = await GetMachinesAsync(cancellationToken);
        return await ExecuteBatchAsync(filter, GetLastMachineIdsByLine(machines, filter.LineName), offset, take, cancellationToken);
    }

    public async Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(
        ProductionReportFilter filter,
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var machineIds = GetLastMachineIdsByLine(await GetMachinesAsync(cancellationToken), filter.LineName);
        var query = dbContext.production_reports.AsNoTracking()
            .Where(report => report.lot_name != null && report.lot_name != string.Empty);
        if (machineIds.Count > 0)
        {
            query = query.Where(report => report.machine_id != null && machineIds.Contains(report.machine_id));
        }
        query = ApplyDateFilter(query, filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        var normalizedSearch = Normalize(search);
        if (normalizedSearch != null)
        {
            query = query.Where(report => EF.Functions.ILike(report.lot_name!, $"%{normalizedSearch}%"));
        }
        var matches = await query.Select(report => report.lot_name!).Distinct().OrderBy(value => value)
            .Take(ReportQueryParameters.ClampOptionLimit(limit))
            .Select(value => new ReportSelectOption { Value = value, Text = value })
            .ToListAsync(cancellationToken);
        return ReportQueryParameters.WithFixedOptions(matches, filter.ModelName);
    }

    private async Task<ReportBatch<ProductionReportRow>> ExecuteBatchAsync(
        ProductionReportFilter filter,
        IReadOnlyList<string> machineIds,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        var (startAt, endAt) = ReportQueryParameters.DateRange(filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        const string sql = """
            WITH grouped AS (
                SELECT
                    COALESCE(mm.line, '') AS "LineName",
                    COALESCE(mm.lane::text, '') AS "Lane",
                    COALESCE(pr.lot_name, '') AS "ModelName",
                    COALESCE(pr.mjs_id, '') AS "GroupName",
                    COALESCE(SUM(pr.count_board), 0)::bigint AS "ProducedQuantityPanel",
                    COALESCE(SUM(pr.count_module), 0)::bigint AS "ProducedQuantityPattern",
                    MIN(pr.report_date) AS "StartTime",
                    MAX(pr.report_date) AS "EndTime"
                FROM production_reports pr
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (cardinality(@machineIds) = 0 OR pr.machine_id = ANY(@machineIds))
                  AND (@modelName IS NULL OR pr.lot_name = @modelName)
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
                GROUP BY mm.line, mm.lane, pr.lot_name, pr.mjs_id
            )
            SELECT "LineName", "Lane", "ModelName", "GroupName", "ProducedQuantityPanel", "ProducedQuantityPattern",
                   "StartTime", "EndTime", COUNT(*) OVER() AS "TotalRecords"
            FROM grouped
            ORDER BY "EndTime" DESC, "LineName", "Lane", "ModelName", "GroupName"
            LIMIT @take OFFSET @offset
            """;
        var sqlRows = await dbContext.Database.SqlQueryRaw<ProductionReportSqlRow>(sql,
            ReportQueryParameters.TextArray("machineIds", machineIds),
            ReportQueryParameters.Text("modelName", filter.ModelName),
            ReportQueryParameters.Timestamp("startAt", startAt),
            ReportQueryParameters.Timestamp("endAt", endAt),
            ReportQueryParameters.Integer("take", Math.Clamp(take, 1, int.MaxValue)),
            ReportQueryParameters.Integer("offset", Math.Max(offset, 0)))
            .ToListAsync(cancellationToken);
        var rows = sqlRows.Select(row => new ProductionReportRow
        {
            LineName = row.LineName,
            Lane = row.Lane,
            ModelName = row.ModelName,
            GroupName = row.GroupName,
            ProducedQuantityPanel = row.ProducedQuantityPanel,
            ProducedQuantityPattern = row.ProducedQuantityPattern,
            StartTime = row.StartTime,
            EndTime = row.EndTime
        }).ToList();
        return ReportQueryParameters.Batch(rows, sqlRows.FirstOrDefault()?.TotalRecords ?? 0, offset);
    }

    private async Task<List<MachineSelection>> GetMachinesAsync(CancellationToken cancellationToken) =>
        await dbContext.master_machines.AsNoTracking().Select(machine => new MachineSelection
        {
            Id = machine.id,
            Line = machine.line,
            MachineName = machine.machine_name,
            Stage = machine.stage
        }).ToListAsync(cancellationToken);

    private static IReadOnlyList<ReportSelectOption> BuildLineOptions(IEnumerable<MachineSelection> machines)
    {
        var values = machines.Select(machine => machine.Line).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!).Distinct().OrderBy(value => value)
            .Select(value => new ReportSelectOption { Value = value, Text = value }).ToList();
        return ReportQueryParameters.WithFixedOptions(values, null);
    }

    private static IQueryable<MPO_Web_Prj.Models.production_report> ApplyDateFilter(
        IQueryable<MPO_Web_Prj.Models.production_report> query,
        DateOnly? startDate,
        TimeOnly? startTime,
        DateOnly? endDate,
        TimeOnly? endTime)
    {
        var (startAt, endAt) = ReportQueryParameters.DateRange(startDate, startTime, endDate, endTime);
        if (startAt.HasValue) query = query.Where(report => report.report_date >= startAt.Value);
        if (endAt.HasValue) query = query.Where(report => report.report_date < endAt.Value);
        return query;
    }

    private static List<string> GetLastMachineIdsByLine(IReadOnlyList<MachineSelection> machines, string? lineName)
    {
        var query = machines.Where(machine => !string.IsNullOrWhiteSpace(machine.Id) && !string.IsNullOrWhiteSpace(machine.Line));
        if (!string.IsNullOrWhiteSpace(lineName)) query = query.Where(machine => machine.Line == lineName);
        return query.GroupBy(machine => machine.Line!).Select(group =>
        {
            var maxStage = group.Max(machine => machine.Stage ?? short.MinValue);
            return group.Where(machine => (machine.Stage ?? short.MinValue) == maxStage)
                .OrderByDescending(machine => GetMachineNumber(machine.MachineName))
                .ThenByDescending(machine => machine.MachineName).ThenByDescending(machine => machine.Id).First().Id;
        }).ToList();
    }

    private static int GetMachineNumber(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return int.MinValue;
        var end = name.Length - 1;
        while (end >= 0 && !char.IsDigit(name[end])) end--;
        if (end < 0) return int.MinValue;
        var start = end;
        while (start >= 0 && char.IsDigit(name[start])) start--;
        return int.TryParse(name.Substring(start + 1, end - start), out var number) ? number : int.MinValue;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void NormalizeFilter(ProductionReportFilter filter)
    {
        filter.LineName = Normalize(filter.LineName);
        filter.ModelName = Normalize(filter.ModelName);
    }
    private static ReportPagination CreatePagination(int totalRecords, bool exportAll) => new()
    {
        Page = 1,
        PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportQueryParameters.BatchSize,
        TotalRecords = totalRecords
    };
    private static ProductionReportViewModel CreateDatabaseErrorViewModel(ProductionReportFilter filter, Exception exception) => new()
    {
        Filter = filter,
        ModelOptions = ReportQueryParameters.SelectedOption(filter.ModelName),
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

    private sealed class MachineSelection
    {
        public string Id { get; init; } = string.Empty;
        public string? Line { get; init; }
        public string? MachineName { get; init; }
        public short? Stage { get; init; }
    }
}
