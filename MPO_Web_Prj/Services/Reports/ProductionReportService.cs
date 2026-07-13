using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public class ProductionReportService : IProductionReportService
{
    private readonly AppDbContext dbContext;

    public ProductionReportService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ProductionReportViewModel> GetReportAsync(ProductionReportFilter filter, CancellationToken cancellationToken)
    {
        var lineOptions = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        lineOptions.AddRange(await dbContext.master_machines
            .AsNoTracking()
            .OrderBy(machine => machine.id)
            .Select(machine => new ReportSelectOption
            {
                Value = machine.id,
                Text = machine.id
            })
            .ToListAsync(cancellationToken));

        var modelOptions = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        modelOptions.AddRange(await dbContext.production_reports
            .AsNoTracking()
            .Where(report => report.lot_name != null && report.lot_name != string.Empty)
            .Select(report => report.lot_name!)
            .Distinct()
            .OrderBy(lotName => lotName)
            .Select(lotName => new ReportSelectOption
            {
                Value = lotName,
                Text = lotName
            })
            .ToListAsync(cancellationToken));

        var query = dbContext.production_reports
            .AsNoTracking()
            .Include(report => report.machine)
            .AsQueryable();

        await SetDefaultDateRangeAsync(filter, cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.LineName))
        {
            query = query.Where(report => report.machine_id == filter.LineName);
        }

        if (!string.IsNullOrWhiteSpace(filter.ModelName))
        {
            query = query.Where(report => report.lot_name == filter.ModelName);
        }

        if (filter.StartDate.HasValue)
        {
            var startTime = filter.StartTime ?? TimeOnly.MinValue;
            var startDateTime = filter.StartDate.Value.ToDateTime(startTime);
            query = query.Where(report => report.report_date >= startDateTime);
        }

        if (filter.EndDate.HasValue)
        {
            var endTime = filter.EndTime ?? TimeOnly.MaxValue;
            var endDateTime = filter.EndDate.Value.ToDateTime(endTime);
            query = query.Where(report => report.report_date <= endDateTime);
        }

        var rows = await query
            .OrderBy(report => report.report_date)
            .ThenBy(report => report.machine_id)
            .Select(report => new ProductionReportRow
            {
                LineName = report.machine_id ?? string.Empty,
                Lane = report.machine != null && report.machine.lane.HasValue
                    ? report.machine.lane.Value.ToString()
                    : string.Empty,
                ModelName = report.lot_name ?? string.Empty,
                GroupName = report.mjs_id ?? string.Empty,
                ProducedQuantityPanel = report.output_qty ?? 0,
                ProducedQuantityPattern = report.count_board ?? 0,
                Time = report.report_date
            })
            .ToListAsync(cancellationToken);

        return new ProductionReportViewModel
        {
            Filter = filter,
            LineOptions = lineOptions,
            ModelOptions = modelOptions,
            Rows = rows
        };
    }

    private async Task SetDefaultDateRangeAsync(ProductionReportFilter filter, CancellationToken cancellationToken)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            return;
        }

        var latestReportDate = await dbContext.production_reports
            .AsNoTracking()
            .Where(report => report.report_date != null)
            .MaxAsync(report => report.report_date, cancellationToken);

        if (!latestReportDate.HasValue)
        {
            return;
        }

        var latestDate = DateOnly.FromDateTime(latestReportDate.Value);
        filter.StartDate = latestDate;
        filter.StartTime = TimeOnly.MinValue;
        filter.EndDate = latestDate;
        filter.EndTime = TimeOnly.MaxValue;
    }
}
