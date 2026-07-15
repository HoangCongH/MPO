using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IPickPlacementByPartService
{
    Task<PickPlacementByPartViewModel> GetReportAsync(PickPlacementByPartFilter filter, CancellationToken cancellationToken);
}
