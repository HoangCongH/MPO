using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByFeederService
{
    Task<PickPlacementByFeederViewModel> GetReportAsync(PickPlacementByFeederFilter filter, CancellationToken cancellationToken);

    Task<PickPlacementByFeederBatch> GetBatchAsync(
        PickPlacementByFeederFilter filter,
        int offset,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(
        string field,
        PickPlacementByFeederFilter filter,
        string? search,
        CancellationToken cancellationToken);
}
