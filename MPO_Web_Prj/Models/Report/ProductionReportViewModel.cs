namespace MPO_Web_Prj.Models.Report;

public class ProductionReportViewModel
{
    public ProductionReportFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> ModelOptions { get; set; } = [];

    public IReadOnlyList<ProductionReportRow> Rows { get; set; } = [];

    public ReportPagination Pagination { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

public class ProductionReportFilter
{
    public bool IsApplied { get; set; }

    public string? LineName { get; set; }

    public string? ModelName { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int Page { get; set; } = 1;
}

public class ReportSelectOption
{
    public string Value { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

public class ProductionReportRow
{
    public string LineName { get; set; } = string.Empty;

    public string Lane { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public long ProducedQuantityPanel { get; set; }

    public long ProducedQuantityPattern { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}
