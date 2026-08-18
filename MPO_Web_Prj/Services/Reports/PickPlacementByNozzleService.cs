using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class PickPlacementByNozzleService : IPickPlacementByNozzleService
{
    private readonly AppDbContext dbContext;

    public PickPlacementByNozzleService(AppDbContext dbContext) => this.dbContext = dbContext;

    public async Task<PickPlacementByNozzleViewModel> GetReportAsync(PickPlacementByNozzleFilter filter, CancellationToken cancellationToken)
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
                return new PickPlacementByNozzleViewModel
                {
                    Filter = filter,
                    LineOptions = lineOptions,
                    MachineNameOptions = machineOptions,
                    StageOptions = stageOptions,
                    PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
                    NozzleSlotOptions = ReportQueryParameters.SelectedOption(filter.NozzleSlot),
                    Pagination = ReportPaging.Create(1, 0),
                    Rows = []
                };
            }

            var batch = await GetBatchAsync(filter, 0, filter.ExportAll ? int.MaxValue : ReportQueryParameters.BatchSize, cancellationToken);
            return new PickPlacementByNozzleViewModel
            {
                Filter = filter,
                LineOptions = lineOptions,
                MachineNameOptions = machineOptions,
                StageOptions = stageOptions,
                PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
                NozzleSlotOptions = ReportQueryParameters.SelectedOption(filter.NozzleSlot),
                Pagination = CreatePagination(batch.TotalRecords, filter.ExportAll),
                Rows = batch.Rows
            };
        }
        catch (Exception ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    public async Task<ReportBatch<PickPlacementByNozzleRow>> GetBatchAsync(
        PickPlacementByNozzleFilter filter,
        int offset,
        int take,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var (startAt, endAt) = ReportQueryParameters.DateRange(filter.StartDate, filter.StartTime, filter.EndDate, filter.EndTime);
        short? stage = short.TryParse(filter.Stage, out var parsedStage) ? parsedStage : null;
        short? nozzleSlot = short.TryParse(filter.NozzleSlot, out var parsedSlot) ? parsedSlot : null;
        const string sql = """
            WITH grouped AS (
                SELECT
                    COALESCE(nl.nozzle_name, '') AS "PartName",
                    COALESCE(mm.line, '') AS "LineName",
                    COALESCE(mm.machine_name, '') AS "MachineName",
                    COALESCE(mm.stage::text, '') AS "Stage",
                    COALESCE(nl.nh_add::text, '') AS "NozzleSlot",
                    COALESCE(nl.nc_add, '') AS "NozzleChangerSlot",
                    COALESCE(SUM(nl.n_pickup_qty), 0)::integer AS "PickupCount",
                    COALESCE(SUM(nl.n_mount_qty), 0)::integer AS "PlacementCount",
                    COALESCE(SUM(nl.n_p_miss_qty), 0)::integer AS "PickupMiss",
                    COALESCE(SUM(nl.n_r_miss_qty), 0)::integer AS "RecogMiss",
                    COALESCE(SUM(nl.n_h_miss_qty), 0)::integer AS "HeightMiss",
                    COALESCE(SUM(nl.n_d_miss_qty), 0)::integer AS "DropMiss",
                    COALESCE(SUM(nl.n_m_miss_qty), 0)::integer AS "MountMiss",
                    COALESCE(SUM(nl.n_trs_miss_qty), 0)::integer AS "TransferMiss",
                    MAX(pr.report_date) AS "LatestReportDate"
                FROM nozzle_logs nl
                INNER JOIN production_reports pr ON pr.id = nl.report_id
                LEFT JOIN master_machines mm ON mm.id = pr.machine_id
                WHERE pr.report_date IS NOT NULL
                  AND (@startAt IS NULL OR pr.report_date >= @startAt)
                  AND (@endAt IS NULL OR pr.report_date < @endAt)
                  AND (@lineName IS NULL OR mm.line = @lineName)
                  AND (@machineName IS NULL OR mm.machine_name = @machineName)
                  AND (@stage IS NULL OR mm.stage = @stage)
                  AND (@partName IS NULL OR nl.nozzle_name = @partName)
                  AND (@nozzleSlot IS NULL OR nl.nh_add = @nozzleSlot)
                GROUP BY nl.nozzle_name, mm.line, mm.machine_name, mm.stage, nl.nh_add, nl.nc_add
            )
            SELECT "PartName", "LineName", "MachineName", "Stage", "NozzleSlot", "NozzleChangerSlot",
                   "PickupCount", "PlacementCount", "PickupMiss", "RecogMiss", "HeightMiss", "DropMiss",
                   "MountMiss", "TransferMiss", COUNT(*) OVER() AS "TotalRecords"
            FROM grouped
            ORDER BY "LatestReportDate" DESC, "LineName", "MachineName", "Stage", "PartName", "NozzleSlot", "NozzleChangerSlot"
            LIMIT @take OFFSET @offset
            """;
        var sqlRows = await dbContext.Database.SqlQueryRaw<PickPlacementByNozzleSqlRow>(sql,
            ReportQueryParameters.Timestamp("startAt", startAt),
            ReportQueryParameters.Timestamp("endAt", endAt),
            ReportQueryParameters.Text("lineName", filter.LineName),
            ReportQueryParameters.Text("machineName", filter.MachineName),
            ReportQueryParameters.SmallInt("stage", stage),
            ReportQueryParameters.Text("partName", filter.PartName),
            ReportQueryParameters.SmallInt("nozzleSlot", nozzleSlot),
            ReportQueryParameters.Integer("take", Math.Clamp(take, 1, int.MaxValue)),
            ReportQueryParameters.Integer("offset", Math.Max(offset, 0)))
            .ToListAsync(cancellationToken);
        var rows = sqlRows.Select(row => new PickPlacementByNozzleRow
        {
            PartName = row.PartName,
            LineName = row.LineName,
            MachineName = row.MachineName,
            Stage = row.Stage,
            NozzleSlot = row.NozzleSlot,
            NozzleChangerSlot = row.NozzleChangerSlot,
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
        string field,
        PickPlacementByNozzleFilter filter,
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);
        var query = ApplyDateFilter(ApplyMachineFilters(dbContext.nozzle_logs.AsNoTracking(), filter), filter);
        IQueryable<string> values = field switch
        {
            "partName" => query.Where(log => log.nozzle_name != null && log.nozzle_name != string.Empty).Select(log => log.nozzle_name!),
            "nozzleSlot" => query.Where(log => log.nh_add != null).Select(log => log.nh_add!.Value.ToString()),
            _ => throw new ArgumentOutOfRangeException(nameof(field), "Unknown nozzle option field.")
        };
        var normalizedSearch = Normalize(search);
        if (normalizedSearch != null) values = values.Where(value => EF.Functions.ILike(value, $"%{normalizedSearch}%"));
        var matches = await values.Distinct().OrderBy(value => value)
            .Take(ReportQueryParameters.ClampOptionLimit(limit))
            .Select(value => new ReportSelectOption { Value = value, Text = value }).ToListAsync(cancellationToken);
        var selected = field == "partName" ? filter.PartName : filter.NozzleSlot;
        return ReportQueryParameters.WithFixedOptions(matches, selected);
    }

    private IQueryable<MPO_Web_Prj.Models.master_machine> BuildMachineOptionQuery(PickPlacementByNozzleFilter filter)
    {
        var query = dbContext.master_machines.AsNoTracking().AsQueryable();
        return filter.LineName == null ? query : query.Where(machine => machine.line == filter.LineName);
    }
    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyMachineFilters(IQueryable<MPO_Web_Prj.Models.nozzle_log> query, PickPlacementByNozzleFilter filter)
    {
        if (filter.LineName != null) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.line == filter.LineName);
        if (filter.MachineName != null) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.machine_name == filter.MachineName);
        if (short.TryParse(filter.Stage, out var stage)) query = query.Where(log => log.report != null && log.report.machine != null && log.report.machine.stage == stage);
        return query;
    }
    private static IQueryable<MPO_Web_Prj.Models.nozzle_log> ApplyDateFilter(IQueryable<MPO_Web_Prj.Models.nozzle_log> query, PickPlacementByNozzleFilter filter)
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
    private static void NormalizeFilter(PickPlacementByNozzleFilter filter)
    {
        filter.LineName = Normalize(filter.LineName);
        filter.MachineName = Normalize(filter.MachineName);
        filter.Stage = Normalize(filter.Stage);
        filter.PartName = Normalize(filter.PartName);
        filter.NozzleSlot = Normalize(filter.NozzleSlot);
    }
    private static ReportPagination CreatePagination(int totalRecords, bool exportAll) => new()
    {
        Page = 1, PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportQueryParameters.BatchSize, TotalRecords = totalRecords
    };
    private static PickPlacementByNozzleViewModel CreateDatabaseErrorViewModel(PickPlacementByNozzleFilter filter, Exception exception) => new()
    {
        Filter = filter,
        PartOptions = ReportQueryParameters.SelectedOption(filter.PartName),
        NozzleSlotOptions = ReportQueryParameters.SelectedOption(filter.NozzleSlot),
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
