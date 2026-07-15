using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface ICycleTimeReportService
{
    Task<CycleTimeReportViewModel> GetReportAsync(CycleTimeReportFilter filter, CancellationToken cancellationToken);
}
