using Microsoft.EntityFrameworkCore;
using MPO_Web_Prj.Data;
using MPO_Web_Prj.Models.Report;
using Npgsql;

namespace MPO_Web_Prj.Services.Reports;

public class BoardCountChartService : IBoardCountChartService
{
    private static readonly TimeOnly StartOfDay = TimeOnly.MinValue;
    private static readonly TimeOnly EndOfDay = new(23, 0);
    private static readonly TimeSpan Shift1Start = TimeSpan.FromHours(6);
    private static readonly TimeSpan Shift2Start = TimeSpan.FromHours(18);

    private readonly AppDbContext dbContext;

    public BoardCountChartService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<BoardCountChartViewModel> GetChartAsync(BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);

        try
        {
            var lineNames = await dbContext.master_machines
                .AsNoTracking()
                .Where(machine => machine.line != null && machine.line != string.Empty)
                .Select(machine => machine.line!)
                .Distinct()
                .OrderBy(line => line)
                .ToListAsync(cancellationToken);

            SetDefaultLines(filter, lineNames);

            if (!filter.IsApplied)
            {
                return new BoardCountChartViewModel
                {
                    Filter = filter,
                    LineOptions = BuildLineOptions(lineNames),
                    Charts = []
                };
            }

            var latestReportDate = await dbContext.production_reports
                .AsNoTracking()
                .Where(report => report.report_date != null)
                .OrderByDescending(report => report.report_date)
                .Select(report => report.report_date)
                .FirstOrDefaultAsync(cancellationToken);

            var hasExplicitDateRange = filter.StartDate.HasValue
                || filter.EndDate.HasValue
                || filter.StartTime.HasValue
                || filter.EndTime.HasValue;
            if (filter.Shift != 4)
            {
                SetDefaultDateRange(filter, latestReportDate);
            }

            var selectedLines = GetSelectedLines(filter);
            var charts = selectedLines.Count == 0
                ? []
                : await BuildChartsAsync(filter, selectedLines, !hasExplicitDateRange, cancellationToken);

            return new BoardCountChartViewModel
            {
                Filter = filter,
                LineOptions = BuildLineOptions(lineNames),
                Charts = charts
            };
        }
        catch (NpgsqlException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (TimeoutException ex)
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
        catch (InvalidOperationException ex) when (IsDatabaseConnectionFailure(ex))
        {
            return CreateDatabaseErrorViewModel(filter, ex);
        }
    }

    private async Task<IReadOnlyList<BoardCountLineChart>> BuildChartsAsync(
        BoardCountChartFilter filter,
        IReadOnlyList<string> selectedLines,
        bool useShiftBuckets,
        CancellationToken cancellationToken)
    {
        var dateRange = ResolveDateTimeRange(filter);
        var startDateTime = dateRange.Start;
        var endDateTime = dateRange.End;
        var isShiftOnlyFilter = filter.Shift is 1 or 2 && useShiftBuckets;
        var bucketType = ResolveBucketType(startDateTime, endDateTime);
        var lastMachineIds = await GetLastMachineIdsByLineAsync(selectedLines, cancellationToken);

        var query = dbContext.production_reports
            .AsNoTracking()
            .Where(report => report.machine != null
                && report.machine.line != null
                && selectedLines.Contains(report.machine.line)
                && report.machine_id != null
                && lastMachineIds.Contains(report.machine_id));

        if (startDateTime.HasValue)
        {
            query = query.Where(report => report.report_date >= startDateTime.Value);
        }

        if (endDateTime.HasValue)
        {
            query = query.Where(report => report.report_date < endDateTime.Value);
        }

        query = ApplyTimeFilter(query, filter.Shift);

        var points = await query
                .Where(report => report.report_date != null)
                .OrderBy(report => report.report_date)
                .ThenBy(report => report.id)
                .Select(report => new
                {
                    LineName = report.machine!.line!,
                    Lane = report.machine.lane,
                    Time = report.report_date!.Value,
                    ModelName = report.lot_name,
                    BoardCount = report.count_board ?? 0
                })
                .ToListAsync(cancellationToken);

        var bucketLabels = isShiftOnlyFilter
            ? BuildShiftBucketLabels(filter.Shift)
            : BuildBucketLabels(bucketType, startDateTime, endDateTime, points.Select(point => point.Time));

        return selectedLines.Select(lineName =>
        {
            var linePoints = points
                .Where(point => point.LineName == lineName
                    && IsSelectedLane(point.Lane, filter.Lane))
                .ToList();

            var series = new[]
            {
                new BoardCountLaneSeries
                {
                    LaneName = filter.Lane == 0 ? "All Lanes" : $"Lane {filter.Lane}",
                Values = bucketLabels
                    .Select(bucket => linePoints
                        .Where(point => GetPointBucketStart(point.Time, bucketType, isShiftOnlyFilter) == bucket.Start)
                        .Sum(point => point.BoardCount))
                    .ToList(),
                    ModelNames = bucketLabels
                        .Select(bucket => string.Join(", ", linePoints
                            .Where(point => GetPointBucketStart(point.Time, bucketType, isShiftOnlyFilter) == bucket.Start)
                            .Select(point => point.ModelName)
                            .Where(modelName => !string.IsNullOrWhiteSpace(modelName))
                            .Select(modelName => modelName!.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)))
                        .ToList()
                }
            };

            return new BoardCountLineChart
            {
                LineName = lineName,
                Labels = bucketLabels.Select(bucket => bucket.Label).ToList(),
                Series = series
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<string>> GetLastMachineIdsByLineAsync(
        IReadOnlyList<string> selectedLines,
        CancellationToken cancellationToken)
    {
        var machines = await dbContext.master_machines
            .AsNoTracking()
            .Where(machine => machine.line != null
                && selectedLines.Contains(machine.line)
                && machine.machine_name != null
                && machine.machine_name != string.Empty)
            .Select(machine => new
            {
                machine.id,
                LineName = machine.line!,
                MachineName = machine.machine_name!
            })
            .ToListAsync(cancellationToken);

        return machines
            .GroupBy(machine => machine.LineName)
            .SelectMany(group =>
            {
                var lastMachineName = group.Max(machine => machine.MachineName);
                return group
                    .Where(machine => machine.MachineName == lastMachineName)
                    .Select(machine => machine.id);
            })
            .ToList();
    }

    private static DateTimeRange ResolveDateTimeRange(BoardCountChartFilter filter)
    {
        DateTimeRange dateRange;

        if (filter.Shift == 4)
        {
            var now = DateTime.Now;
            dateRange = new DateTimeRange(now.AddHours(-1), now);
            filter.ResolvedStartDateTime = dateRange.Start;
            filter.ResolvedEndDateTime = dateRange.End;
            return dateRange;
        }

        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
        {
            dateRange = new DateTimeRange(null, null);
            filter.ResolvedStartDateTime = dateRange.Start;
            filter.ResolvedEndDateTime = dateRange.End;
            return dateRange;
        }

        var hasExplicitTime = filter.StartTime.HasValue || filter.EndTime.HasValue;
        if (hasExplicitTime)
        {
            dateRange = new DateTimeRange(
                filter.StartDate.Value.ToDateTime(filter.StartTime ?? StartOfDay),
                filter.EndDate.Value.ToDateTime(filter.EndTime ?? EndOfDay));
            filter.ResolvedStartDateTime = dateRange.Start;
            filter.ResolvedEndDateTime = dateRange.End;
            return dateRange;
        }

        dateRange = filter.Shift switch
        {
            1 => new DateTimeRange(
                filter.StartDate.Value.ToDateTime(TimeOnly.FromTimeSpan(Shift1Start)),
                filter.EndDate.Value.ToDateTime(TimeOnly.FromTimeSpan(Shift2Start))),
            2 => new DateTimeRange(
                filter.StartDate.Value.ToDateTime(TimeOnly.FromTimeSpan(Shift2Start)),
                filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.FromTimeSpan(Shift1Start))),
            3 => new DateTimeRange(
                filter.StartDate.Value.ToDateTime(StartOfDay),
                filter.EndDate.Value.AddDays(1).ToDateTime(StartOfDay)),
            _ => new DateTimeRange(
                filter.StartDate.Value.ToDateTime(StartOfDay),
                filter.EndDate.Value.ToDateTime(EndOfDay).AddHours(1))
        };
        filter.ResolvedStartDateTime = dateRange.Start;
        filter.ResolvedEndDateTime = dateRange.End;
        return dateRange;
    }

    private static IQueryable<MPO_Web_Prj.Models.production_report> ApplyTimeFilter(
        IQueryable<MPO_Web_Prj.Models.production_report> query,
        int shift)
    {
        return shift switch
        {
            1 => query.Where(report => report.report_date != null
                && report.report_date.Value.TimeOfDay >= Shift1Start
                && report.report_date.Value.TimeOfDay < Shift2Start),
            2 => query.Where(report => report.report_date != null
                && (report.report_date.Value.TimeOfDay >= Shift2Start
                    || report.report_date.Value.TimeOfDay < Shift1Start)),
            3 or 4 => query,
            _ => query
        };
    }

    private static BucketType ResolveBucketType(DateTime? startDateTime, DateTime? endDateTime)
    {
        if (!startDateTime.HasValue || !endDateTime.HasValue)
        {
            return BucketType.Hour;
        }

        var range = endDateTime.Value - startDateTime.Value;

        if (range.TotalHours <= 24)
        {
            return BucketType.Hour;
        }

        if (range.TotalDays <= 31)
        {
            return BucketType.Day;
        }

        return range.TotalDays <= 365
            ? BucketType.Month
            : BucketType.Year;
    }

    private static List<BucketLabel> BuildBucketLabels(
        BucketType bucketType,
        DateTime? startDateTime,
        DateTime? endDateTime,
        IEnumerable<DateTime> dataTimes)
    {
        if (!startDateTime.HasValue || !endDateTime.HasValue)
        {
            return dataTimes
                .Select(time => GetBucketStart(time, bucketType))
                .Distinct()
                .OrderBy(time => time)
                .Select(time => new BucketLabel(time, FormatBucketLabel(time, bucketType)))
                .ToList();
        }

        var labels = new List<BucketLabel>();
        var current = GetBucketStart(startDateTime.Value, bucketType);
        var end = GetBucketStart(endDateTime.Value.AddTicks(-1), bucketType);

        while (current <= end)
        {
            labels.Add(new BucketLabel(current, FormatBucketLabel(current, bucketType)));
            current = AddBucket(current, bucketType);
        }

        return labels;
    }

    private static DateTime GetBucketStart(DateTime value, BucketType bucketType)
    {
        return bucketType switch
        {
            BucketType.Hour => new DateTime(value.Year, value.Month, value.Day, value.Hour, 0, 0),
            BucketType.Day => value.Date,
            BucketType.Month => new DateTime(value.Year, value.Month, 1),
            BucketType.Year => new DateTime(value.Year, 1, 1),
            _ => value
        };
    }

    private static DateTime GetPointBucketStart(DateTime value, BucketType bucketType, bool useShiftHour)
    {
        return useShiftHour
            ? new DateTime(2000, 1, 1, value.Hour, 0, 0)
            : GetBucketStart(value, bucketType);
    }

    private static List<BucketLabel> BuildShiftBucketLabels(int shift)
    {
        var hours = shift == 2
            ? Enumerable.Range(18, 6).Concat(Enumerable.Range(0, 6))
            : Enumerable.Range(6, 12);

        return hours
            .Select(hour =>
            {
                var bucketStart = new DateTime(2000, 1, 1, hour, 0, 0);
                return new BucketLabel(bucketStart, bucketStart.ToString("HH:00"));
            })
            .ToList();
    }
    private static DateTime AddBucket(DateTime value, BucketType bucketType)
    {
        return bucketType switch
        {
            BucketType.Hour => value.AddHours(1),
            BucketType.Day => value.AddDays(1),
            BucketType.Month => value.AddMonths(1),
            BucketType.Year => value.AddYears(1),
            _ => value
        };
    }

    private static string FormatBucketLabel(DateTime value, BucketType bucketType)
    {
        return bucketType switch
        {
            BucketType.Hour => value.ToString("HH:00"),
            BucketType.Day => value.ToString("MM/dd"),
            BucketType.Month => value.ToString("yyyy-MM"),
            BucketType.Year => value.ToString("yyyy"),
            _ => value.ToString("yyyy-MM-dd HH:mm")
        };
    }

    private static void SetDefaultLines(BoardCountChartFilter filter, IReadOnlyList<string> lineNames)
    {
        var lines = new[] { filter.Line1, filter.Line2, filter.Line3, filter.Line4 };

        filter.Line1 = ResolveLine(lines[0], lineNames, 0);
        filter.Line2 = ResolveLine(lines[1], lineNames, 1);
        filter.Line3 = ResolveLine(lines[2], lineNames, 2);
        filter.Line4 = ResolveLine(lines[3], lineNames, 3);
    }

    private static string? ResolveLine(string? requestedLine, IReadOnlyList<string> lineNames, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(requestedLine) && lineNames.Contains(requestedLine))
        {
            return requestedLine;
        }

        return lineNames.Count > fallbackIndex
            ? lineNames[fallbackIndex]
            : lineNames.FirstOrDefault();
    }

    private static IReadOnlyList<string> GetSelectedLines(BoardCountChartFilter filter)
    {
        return new[] { filter.Line1, filter.Line2, filter.Line3, filter.Line4 }
            .Take(filter.Type)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line!)
            .ToList();
    }

    private static bool IsSelectedLane(short? lane, int selectedLane)
    {
        return selectedLane == 0
            ? lane is 1 or 2
            : lane == selectedLane;
    }

    private static List<ReportSelectOption> BuildLineOptions(IEnumerable<string> lineNames)
    {
        return lineNames.Select(lineName => new ReportSelectOption
        {
            Value = lineName,
            Text = lineName
        }).ToList();
    }

    private static void NormalizeFilter(BoardCountChartFilter filter)
    {
        filter.Type = Math.Clamp(filter.Type, 1, 4);
        filter.Lane = filter.Lane is 1 or 2 ? filter.Lane : 0;
        filter.Shift = filter.Shift is >= 1 and <= 4 ? filter.Shift : 1;
        filter.Line1 = Normalize(filter.Line1);
        filter.Line2 = Normalize(filter.Line2);
        filter.Line3 = Normalize(filter.Line3);
        filter.Line4 = Normalize(filter.Line4);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void SetDefaultDateRange(BoardCountChartFilter filter, DateTime? latestReportDate)
    {
        if (filter.StartDate.HasValue || filter.EndDate.HasValue || !latestReportDate.HasValue)
        {
            return;
        }

        var latestDate = DateOnly.FromDateTime(latestReportDate.Value);
        filter.StartDate = latestDate;
        filter.StartTime = StartOfDay;
        filter.EndDate = latestDate;
        filter.EndTime = EndOfDay;
    }

    private enum BucketType
    {
        Hour,
        Day,
        Month,
        Year
    }

    private sealed record BucketLabel(DateTime Start, string Label);

    private sealed record DateTimeRange(DateTime? Start, DateTime? End);

    private static BoardCountChartViewModel CreateDatabaseErrorViewModel(BoardCountChartFilter filter, Exception exception)
    {
        return new BoardCountChartViewModel
        {
            Filter = filter,
            ErrorMessage = $"Cannot connect to PostgreSQL database. Please check the DB server/IP, network/VPN, port 5432, database name, username and password. Detail: {exception.Message}"
        };
    }

    private static bool IsDatabaseConnectionFailure(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is NpgsqlException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
