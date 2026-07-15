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

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? Line4 { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }
}

public class BoardCountLineChart
{
    public string LineName { get; set; } = string.Empty;

    public IReadOnlyList<string> Labels { get; set; } = [];

    public IReadOnlyList<BoardCountLaneSeries> Series { get; set; } = [];
}

public class BoardCountLaneSeries
{
    public string LaneName { get; set; } = string.Empty;

    public IReadOnlyList<int> Values { get; set; } = [];
}
