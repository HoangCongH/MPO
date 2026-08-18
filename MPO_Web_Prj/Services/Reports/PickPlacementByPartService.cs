using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class PickPlacementByPartService : IPickPlacementByPartService
{
    private readonly AppDbContext dbContext;

    public PickPlacementByPartService(AppDbContext dbContext) => this.dbContext = dbContext;

    public async Task<PickPlacementByPartViewModel> GetReportAsync(PickPlacementByPartFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        try
        {
            var lineOptions = await BuildOptionsAsync(dbContext.master_machines.AsNoTracking()
                .Where(machine => machine.line != null && machine.line != string.Empty).Select(machine => machine.line!), cancellationToken);
            var machineQuery = BuildMachineOptionQuery(filter);
            var machineOptions = await BuildOptionsAsync(machineQuery
                .Where(machine => machine.machine_name != null && machine.machine_name != string.Empty).Select(machine => machine.machine_name!), cancellationToken);
            var stageOptions = await BuildOptionsAsync(machineQuery.Where(machine => machine.stage != null)
                .Select(machine => machine.stage!.Value.ToString()), cancellationToken);
            if (!filter.IsApplied)
            {
                return new PickPlacementByPartViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    MachineNameOptions = machineOptions,
                    StageOptions = stageOptions,
                    PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
                    Pagination = ReportPaging.Create(1, 0),
                    Rows = []
                };
            }

            var batch = await GetBatchAsync(filter, 0, filter.ExportAll ? int.MaxValue : ReportQueryParameters.BatchSize, cancellationToken);
            return new PickPlacementByPartViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                MachineNameOptions = machineOptions,
                StageOptions = stageOptions,
                PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
                Pagination = CreatePagination(batch.TotalRecords, filter.ExportAll),
                Rows = batch.Rows
            };
        }
        catch (Exception ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    public async Task<ReportBatch<PickPlacementByPartRow>> GetBatchAsync(
        PickPlacementByPartFilter filter,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var (startAt, endAt) = ReportQueryParameters.DateRange(filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        short? stage = short.TryParse(filter.Stage, out var parsedStage) ? parsedStage : null;
        const string sql = """
            WITH grouped AS (
                SELECT
                    COALESCE(mm.line, '') AS "LineName",
                    COALESCE(mm.machine_name, '') AS "MachineName",
                    COALESCE(mm.stage::text, '') AS "Stage",
                    COALESCE(fl.part_name, '') AS "PartName",
                    COALESCE(SUM(fl.f_pickup_qty), 0)::integer AS "PickupCount",
                    COALESCE(SUM(fl.f_mount_qty), 0)::integer AS "PlacementCount",
                    COALESCE(SUM(fl.f_p_miss_qty), 0)::integer AS "PickupMiss",
                    COALESCE(SUM(fl.f_r_miss_qty), 0)::integer AS "RecogMiss",
                    COALESCE(SUM(fl.f_h_miss_qty), 0)::integer AS "HeightMiss",
                    COALESCE(SUM(fl.f_d_miss_qty), 0)::integer AS "DropMiss",
                    COALESCE(SUM(fl.f_m_miss_qty), 0)::integer AS "MountMiss",
                    COALESCE(SUM(fl.f_trs_miss_qty), 0)::integer AS "TransferMiss",
                    MAX(pr.report_date) AS "LatestReportDate"
                FROM feeder_logs fl
                INNER JOIN production_reports pr ON pr.id = fl.report_id
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
                  AND (@lineName IS NULL OR mm.line = @lineName)
                  AND (@machineName IS NULL OR mm.machine_name = @machineName)
                  AND (@stage IS NULL OR mm.stage = @stage)
                  AND (@partName IS NULL OR fl.part_name = @partName)
                GROUP BY mm.line, mm.machine_name, mm.stage, fl.part_name
            )
            SELECT "LineName", "MachineName", "Stage", "PartName", "PickupCount", "PlacementCount",
                   "PickupMiss", "RecogMiss", "HeightMiss", "DropMiss", "MountMiss", "TransferMiss",
                   COUNT(*) OVER() AS "TotalRecords"
            FROM grouped
            ORDER BY "LatestReportDate" DESC, "PickupCount" DESC, "LineName", "MachineName", "Stage", "PartName"
            LIMIT @take OFFSET @offset
            """;
        var sqlRows = await dbContext.Database.SqlQueryRaw<PickPlacementByPartSqlRow>(sql,
            ReportQueryParameters.Timestamp("startAt", startAt),
            ReportQueryParameters.Timestamp("endAt", endAt),
            ReportQueryParameters.Text("lineName", filter.LineName),
            ReportQueryParameters.Text("machineName", filter.MachineName),
            ReportQueryParameters.SmallInt("stage", stage),
            ReportQueryParameters.Text("partName", filter.PartName),
            ReportQueryParameters.Integer("take", Math.Clamp(take, 1, int.MaxValue)),
            ReportQueryParameters.Integer("offset", Math.Max(offset, 0)))
            .ToListAsync(cancellationToken);
        var rows = sqlRows.Select(row => new PickPlacementByPartRow
        {
            LineName = row.LineName,
            MachineName = row.MachineName,
            Stage = row.Stage,
            PartName = row.PartName,
            PickupCount = row.PickupCount,
            PlacementCount = row.PlacementCount,
            PickupMiss = row.PickupMiss,
            RecogMiss = row.RecogMiss,
            HeightMiss = row.HeightMiss,
            DropMiss = row.DropMiss,
            MountMiss = row.MountMiss,
            TransferMiss = row.TransferMiss,
            ScrapRatio = ScrapRatio(row.PickupCount, row.PlacementCount)
        }).ToList();
        return ReportQueryParameters.Batch(rows, sqlRows.FirstOrDefault()?.TotalRecords ?? 0, offset);
    }

    public async Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(
        PickPlacementByPartFilter filter,
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var query = ApplyDateFilter(ApplyMachineFilters(dbContext.feeder_logs.AsNoTracking(), filter), filter);
        var normalizedSearch = Normalize(search);
        var values = query.Where(log => log.part_name != null && log.part_name != string.Empty);
        if (normalizedSearch != null) values = values.Where(log => EF.Functions.ILike(log.part_name!, $"%{normalizedSearch}%"));
        var matches = await values.Select(log => log.part_name!).Distinct().OrderBy(value => value)
            .Take(ReportQueryParameters.ClampOptionLimit(limit))
            .Select(value => new ReportSelectOption { Value = value, Text = value })
            .ToListAsync(cancellationToken);
        return ReportQueryParameters.WithFixedOptions(matches, filter.PartName);
    }

    private IQueryable<MPO_Web_Prj.Models.master_machine> BuildMachineOptionQuery(PickPlacementByPartFilter filter)
    {
        var query = dbContext.master_machines.AsNoTracking().AsQueryable();
        return filter.LineName == null ? query : query.Where(machine => machine.line == filter.LineName);
    }

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyMachineFilters(
        IQueryable<MPO_Web_Prj.Models.feeder_log> query,
        PickPlacementByPartFilter filter)
    {
        if (filter.LineName != null) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.line == filter.LineName);
        if (filter.MachineName != null) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.machine_name == filter.MachineName);
        if (short.TryParse(filter.Stage, out var stage)) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.stage == stage);
        return query;
    }

    private static IQueryable<MPO_Web_Prj.Models.feeder_log> ApplyDateFilter(IQueryable<MPO_Web_Prj.Models.feeder_log> query, PickPlacementByPartFilter filter)
    {
        var (startAt, endAt) = ReportQueryParameters.DateRange(filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        if (startAt.HasValue) query = query.Where(log => log.report != null && log.report.report_date >= startAt.Value);
        if (endAt.HasValue) query = query.Where(log => log.report != null && log.report.report_date < endAt.Value);
        return query;
    }

    private static async Task<IReadOnlyList<ReportSelectOption>> BuildOptionsAsync(IQueryable<string> values, CancellationToken cancellationToken)
    {
        var matches = await values.Distinct().OrderBy(value => value)
            .Select(value => new ReportSelectOption { Value = value, Text = value }).ToListAsync(cancellationToken);
        return ReportQueryParameters.WithFixedOptions(matches, null);
    }

    private static decimal ScrapRatio(int pickup, int placement) => pickup <= 0 ? 0 : decimal.Round(1_000_000m * (1 - (decimal)placement / pickup), 2);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void NormalizeFilter(PickPlacementByPartFilter filter)
    {
        filter.LineName = Normalize(filter.LineName);
        filter.MachineName = Normalize(filter.MachineName);
        filter.Stage = Normalize(filter.Stage);
        filter.PartName = Normalize(filter.PartName);
    }
    private static ReportPagination CreatePagination(int totalRecords, bool exportAll) => new()
    {
        Page = 1, PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportQueryParameters.BatchSize, TotalRecords = totalRecords
    };
    private static PickPlacementByPartViewModel CreateDatabaseErrorViewModel(PickPlacementByPartFilter filter, Exception exception) => new()
    {
        Filter = filter,
        PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
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
