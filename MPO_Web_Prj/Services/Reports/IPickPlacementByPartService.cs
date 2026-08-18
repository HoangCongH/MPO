using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByPartService
{
    Task<PickPlacementByPartViewModel> GetReportAsync(PickPlacementByPartFilter filter, CancellationToken cancellationToken);
    Task<ReportBatch<PickPlacementByPartRow>> GetBatchAsync(PickPlacementByPartFilter filter, int offset, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(PickPlacementByPartFilter filter, string? search, int limit, CancellationToken cancellationToken);
}
