using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByNozzleService
{
    Task<PickPlacementByNozzleViewModel> GetReportAsync(PickPlacementByNozzleFilter filter, CancellationToken cancellationToken);
}
