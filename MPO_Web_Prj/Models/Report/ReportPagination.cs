namespace MPO_Web_Prj.Models.Report;

public class ReportPagination
{
    public const int DefaultPageSize = 500;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public int TotalRecords { get; set; }

    public int TotalPages => TotalRecords <= 0
        ? 1
        : (int)Math.Ceiling((double)TotalRecords / PageSize);

    public int Skip => (Math.Max(Page, 1) - 1) * PageSize;
}

public class ReportPaginationViewModel
{
    public ReportPagination Pagination { get; set; } = new();
}
