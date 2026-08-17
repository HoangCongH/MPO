namespace MPO_Web_Prj.Models.Report;

public class PickPlacementByFeederViewModel
{
    public PickPlacementByFeederFilter Filter { get; set; } = new();

    public IReadOnlyList<ReportSelectOption> LineOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> MachineNameOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> StageOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> PartOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> FeederIdOptions { get; set; } = [];

    public IReadOnlyList<ReportSelectOption> FeederSlotOptions { get; set; } = [];

    public IReadOnlyList<PickPlacementByFeederRow> Rows { get; set; } = [];

    public ReportPagination Pagination { get; set; } = new();

    public string? ErrorMessage { get; set; }
}

public class PickPlacementByFeederFilter
{
    public bool IsApplied { get; set; }

    public string? LineName { get; set; }

    public string? MachineName { get; set; }

    public string? Stage { get; set; }

    public string? PartName { get; set; }

    public string? FeederId { get; set; }

    public string? FeederSlot { get; set; }

    public DateOnly? StartDate { get; set; }

    public TimeOnly? StartTime { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int Page { get; set; } = 1;

    public bool ExportAll { get; set; }
}

public class PickPlacementByFeederRow
{
    public string PartName { get; set; } = string.Empty;

    public string LineName { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string FeederId { get; set; } = string.Empty;

    public string FeederTable { get; set; } = string.Empty;

    public string FeederSlot { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public int PickupCount { get; set; }

    public int PlacementCount { get; set; }

    public int PickupMiss { get; set; }

    public int RecogMiss { get; set; }

    public int HeightMiss { get; set; }

    public int DropMiss { get; set; }

    public int MountMiss { get; set; }

    public int TransferMiss { get; set; }

    public decimal ScrapRatio { get; set; }

    public bool HasPickupWithoutPlacement => PickupCount > 0 && PlacementCount == 0;
}

public class PickPlacementByFeederBatch
{
    public IReadOnlyList<PickPlacementByFeederRow> Rows { get; init; } = [];

    public int TotalRecords { get; init; }

    public int NextOffset { get; init; }

    public bool HasMore { get; init; }
}

// Projection used by the window-count SQL query. It is not an EF entity.
public sealed class PickPlacementByFeederSqlRow
{
    public string PartName { get; init; } = string.Empty;
    public string LineName { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public string FeederId { get; init; } = string.Empty;
    public string FeederAdd { get; init; } = string.Empty;
    public short? FeederSubAdd { get; init; }
    public int PickupCount { get; init; }
    public int PlacementCount { get; init; }
    public int PickupMiss { get; init; }
    public int RecogMiss { get; init; }
    public int HeightMiss { get; init; }
    public int DropMiss { get; init; }
    public int MountMiss { get; init; }
    public int TransferMiss { get; init; }
    public long TotalRecords { get; init; }
}
