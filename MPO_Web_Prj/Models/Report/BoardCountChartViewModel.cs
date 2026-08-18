namespace MPO_Web_Prj.Models.Report;

public class BoardCountChartViewModel
{
    public BoardCountChartFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<BoardCountLineChart> Charts { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class BoardCountChartFilter
{
    public bool IsApplied { get; set; }

    public int Type { get; set; } = 1;

    // 0 represents the combined view of Lane 1 and Lane 2.
    public int Lane { get; set; }

    // Overall Dashboard needs the two lanes side by side; the Board Count page
    // retains its explicit Lane/All Lanes filter behavior.
    public bool SplitLanes { get; set; }

    public bool AutoRefresh { get; set; }

    // Sent by the browser during polling so active-shift detection is not affected
    // by a container running in a different time zone.
    public DateTime? ClientNow { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? Line4 { get; set; }

    public int Shift { get; set; } = 1;

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }

    public DateTime? ResolvedStartDateTime { get; set; }

    public DateTime? ResolvedEndDateTime { get; set; }
}

public class BoardCountLineChart
{
    public string LineName { get; set; } = string.Empty;

    public IReadOnlyList<string> Labels { get; set; } = [];

    public IReadOnlyList<BoardCountLaneSeries> Series { get; set; } = [];

    // The Board Count page renders these as stacked model segments. Overall Dashboard
    // continues to use Series so its Board Produced calculation is unchanged.
    public IReadOnlyList<BoardCountModelSeries> ModelSeries { get; set; } = [];
}

public class BoardCountLaneSeries
{
    public string LaneName { get; set; } = string.Empty;

    public IReadOnlyList<int> Values { get; set; } = [];

    // Matches Values by bucket index and is used by the chart tooltip.
    public IReadOnlyList<string> ModelNames { get; set; } = [];
}

public class BoardCountModelSeries
{
    public string ModelName { get; set; } = string.Empty;

    public IReadOnlyList<int> Values { get; set; } = [];
}
