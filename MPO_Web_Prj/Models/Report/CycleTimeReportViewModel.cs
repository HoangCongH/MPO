namespace MPO_Web_Prj.Models.Report;

public class CycleTimeReportViewModel
{
    public CycleTimeReportFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> ModelOptions { get; set; } = [];

    public IReadOnlyList<CycleTimeReportRow> Rows { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class CycleTimeReportFilter
{
    public bool IsApplied { get; set; }

    public string? LineName { get; set; }

    public string? ModelName { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }
}

public class CycleTimeReportRow
{
    public string LineName { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public decimal CycleTime1 { get; set; }

    public decimal CycleTime2 { get; set; }

    public decimal CycleTime3 { get; set; }
}
