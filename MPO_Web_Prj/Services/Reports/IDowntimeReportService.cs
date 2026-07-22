using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface IDowntimeReportService
{
    Task<DowntimeReportViewModel> GetReportAsync(DowntimeReportFilter filter, CancellationToken cancellationToken);
}
