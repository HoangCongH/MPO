using Npgsql;
using NpgsqlTypes;
using MPO_Web_Prj.Models.Report;

namespace MPO_Web_Prj.Services.Reports;

internal static class ReportQueryParameters
{
    internal const int BatchSize = ReportPagination.DefaultPageSize;
    internal const int OptionLimit = 50;

    internal static (DateTime? StartAt, DateTime? EndAt) DateRange(
        DateOnly? startDate,
        TimeOnly? startTime,
        DateOnly? endDate,
        TimeOnly? endTime)
    {
        var startAt = startDate?.ToDateTime(startTime ?? TimeOnly.MinValue);
        DateTime? endAt = null;

        if (endDate.HasValue)
        {
            var endOfDay = new TimeOnly(23, 59, 59);
            endAt = endTime.HasValue && endTime.Value != endOfDay
                ? endDate.Value.ToDateTime(endTime.Value).AddTicks(1)
                : endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
        }

        return (startAt, endAt);
    }

    internal static NpgsqlParameter Text(string name, string? value) =>
        new(name, NpgsqlDbType.Text) { Value = (object?)value ?? DBNull.Value };

    internal static NpgsqlParameter Timestamp(string name, DateTime? value) =>
        new(name, NpgsqlDbType.Timestamp) { Value = (object?)value ?? DBNull.Value };

    internal static NpgsqlParameter SmallInt(string name, short? value) =>
        new(name, NpgsqlDbType.Smallint) { Value = (object?)value ?? DBNull.Value };

    internal static NpgsqlParameter TextArray(string name, IReadOnlyList<string> values) =>
        new(name, NpgsqlDbType.Array | NpgsqlDbType.Text) { Value = values.ToArray() };

    internal static NpgsqlParameter Integer(string name, int value) =>
        new(name, NpgsqlDbType.Integer) { Value = value };

    internal static int ClampOptionLimit(int limit) => Math.Clamp(limit, 1, OptionLimit);

    internal static ReportBatch<TRow> Batch<TRow>(IReadOnlyList<TRow> rows, long totalRecords, int offset)
    {
        var total = checked((int)totalRecords);
        var nextOffset = Math.Max(offset, 0) + rows.Count;
        return new ReportBatch<TRow>
        {
            Rows = rows,
            TotalRecords = total,
            NextOffset = nextOffset,
            HasMore = nextOffset < total
        };
    }

    internal static IReadOnlyList<ReportSelectOption> SelectedOption(string? value)
    {
        var options = new List<ReportSelectOption>
        {
            new() { Value = string.Empty, Text = "All" }
        };

        if (!string.IsNullOrWhiteSpace(value))
        {
            options.Add(new ReportSelectOption { Value = value, Text = value });
        }

        return options;
    }

    internal static IReadOnlyList<ReportSelectOption> WithFixedOptions(
        IReadOnlyList<ReportSelectOption> matches,
        string? selectedValue)
    {
        var options = matches.ToList();
        if (!string.IsNullOrWhiteSpace(selectedValue)
            && options.All(option => option.Value != selectedValue))
        {
            options.Insert(0, new ReportSelectOption { Value = selectedValue, Text = selectedValue });
        }

        options.Insert(0, new ReportSelectOption { Value = string.Empty, Text = "All" });
        return options;
    }
}
