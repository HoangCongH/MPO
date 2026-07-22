namespace MPO_Web_Prj.Models.Report;

public class TotalPickupPlacementReportViewModel
{
    public TotalPickupPlacementReportFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<TotalPickupPlacementReportRow> Rows { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class TotalPickupPlacementReportFilter
{
    public bool IsApplied { get; set; }

    public string? LineName { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }
}

public class TotalPickupPlacementReportRow
{
    public string LineName { get; set; } = string.Empty;

    public long TotalPickup { get; set; }

    public long TotalPlacement { get; set; }

    public decimal Ppm { get; set; }
}
