namespace MPO_Web_Prj.Models.Report;

public class DowntimeReportViewModel
{
    public DowntimeReportFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<DowntimeReportRow> Rows { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class DowntimeReportFilter
{
    public bool IsApplied { get; set; }

    public string? LineName { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }
}

public class DowntimeReportRow
{
    public string LineName { get; set; } = string.Empty;

    public long ChipPickupErrorCount { get; set; }

    public decimal ChipPickupErrorTime { get; set; }

    public long ChipRecogErrorCount { get; set; }

    public decimal ChipRecogErrorTime { get; set; }

    public long SingleErrorStopCount { get; set; }

    public decimal SingleErrorStopTime { get; set; }

    public long TroubleStopCount { get; set; }

    public decimal TroubleStopTime { get; set; }

    public long PartExhaustStopCount { get; set; }

    public decimal PartExhaustStopTime { get; set; }
}
