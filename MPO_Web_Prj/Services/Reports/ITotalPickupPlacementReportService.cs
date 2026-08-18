using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface ITotalPickupPlacementReportService
{
    Task<TotalPickupPlacementReportViewModel> GetReportAsync(TotalPickupPlacementReportFilter filter, CancellationToken cancellationToken);
    Task<ReportBatch<TotalPickupPlacementReportRow>> GetBatchAsync(TotalPickupPlacementReportFilter filter, int offset, int take, CancellationToken cancellationToken);
}
