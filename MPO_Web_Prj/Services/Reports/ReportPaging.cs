using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public static class ReportPaging
{
    public static ReportPagination Create(int requestedPage, int totalRecords, bool exportAll = false)
    {
        var pagination = new ReportPagination
        {
            Page = Math.Max(requestedPage, 1),
            TotalRecords = Math.Max(totalRecords, 0),
            // Export receives all matching records; the interactive table starts with one 200-row batch.
            PageSize = exportAll ? Math.Max(totalRecords, 1) : ReportPagination.DefaultPageSize
        };

        if (pagination.Page > pagination.TotalPages)
        {
            pagination.Page = pagination.TotalPages;
        }

        return pagination;
    }
}
