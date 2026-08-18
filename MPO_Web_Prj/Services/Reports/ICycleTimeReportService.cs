using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public interface ICycleTimeReportService
{
    Task<CycleTimeReportViewModel> GetReportAsync(CycleTimeReportFilter filter, CancellationToken cancellationToken);
    Task<ReportBatch<CycleTimeReportRow>> GetBatchAsync(CycleTimeReportFilter filter, int offset, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportSelectOption>> GetFilterOptionsAsync(CycleTimeReportFilter filter, string? search, int limit, CancellationToken cancellationToken);
}
