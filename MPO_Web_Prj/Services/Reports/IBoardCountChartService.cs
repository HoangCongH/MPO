using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IBoardCountChartService
{
    Task<BoardCountChartViewModel> GetChartAsync(BoardCountChartFilter filter, CancellationToken cancellationToken);
}
