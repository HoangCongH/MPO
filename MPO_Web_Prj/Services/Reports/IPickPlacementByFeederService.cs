using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByFeederService
{
    Task<PickPlacementByFeederViewModel> GetReportAsync(PickPlacementByFeederFilter filter, CancellationToken cancellationToken);
}
