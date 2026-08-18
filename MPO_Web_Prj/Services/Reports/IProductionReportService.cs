using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IProductionReportService
{
    Task<ProductionReportViewModel> GetReportAsync(ProductionReportFilter filter, CancellationToken cancellationToken);
    Task<ReportBatch<ProductionReportRow>> GetBatchAsync(ProductionReportFilter filter, int offset, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(ProductionReportFilter filter, string? search, int limit, CancellationToken cancellationToken);
}
