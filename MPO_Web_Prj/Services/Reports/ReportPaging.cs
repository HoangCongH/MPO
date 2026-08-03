using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

public static class ReportPaging
{
    public static ReportPagination Create(int requestedPage, int totalRecords)
    {
        var pagination = new ReportPagination
        {
            Page = Math.Max(requestedPage, 1),
            TotalRecords = Math.Max(totalRecords, 0)
        };

        if (pagination.Page > pagination.TotalPages)
        {
            pagination.Page = pagination.TotalPages;
        }

        return pagination;
    }
}
