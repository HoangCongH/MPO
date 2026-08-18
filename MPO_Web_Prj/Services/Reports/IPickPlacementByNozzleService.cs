using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByNozzleService
{
    Task<PickPlacementByNozzleViewModel> GetReportAsync(PickPlacementByNozzleFilter filter, CancellationToken cancellationToken);
    Task<ReportBatch<PickPlacementByNozzleRow>> GetBatchAsync(PickPlacementByNozzleFilter filter, int offset, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(string field, PickPlacementByNozzleFilter filter, string? search, int limit, CancellationToken cancellationToken);
}
