namespace MPO_Web_Prj.Models.Report;

public class OverallDashboardViewModel
{
    public BoardCountChartViewModel BoardProduced { get; set; } = new();

    public IReadOnlyList<OverallErrorStopSlice> ErrorStopTime { get; set; } = [];

    public IReadOnlyList<OverallWorstFeederRow> TopWorstFeeders { get; set; } = [];

    public IReadOnlyList<OverallWorstNozzleRow> TopWorstNozzles { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class OverallErrorStopSlice
{
    public string Label { get; set; } = string.Empty;

    public decimal Value { get; set; }
}

public class OverallWorstFeederRow
{
    public string FeederId { get; set; } = string.Empty;

    public string FeederSlot { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public long TotalPickup { get; set; }

    public long TotalPlacement { get; set; }

    public decimal Ppm { get; set; }
}

public class OverallWorstNozzleRow
{
    public string NozzleSlot { get; set; } = string.Empty;

    public string Head { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public long TotalPickup { get; set; }

    public long TotalPlacement { get; set; }

    public decimal Ppm { get; set; }
}
