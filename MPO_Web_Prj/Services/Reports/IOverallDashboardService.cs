using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IOverallDashboardService
{
    Task<OverallDashboardViewModel> GetDashboardAsync(BoardCountChartFilter filter, CancellationToken cancellationToken);
}
