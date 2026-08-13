namespace MPO_Web_Prj.Services.Reports;

public static class ReportFilterGuard
{
    public const string RequiredDateTimeMessage = "Please enter Start date, Start time, End date, and End time before applying the filter.";

    public static bool HasRequiredDateTime(object filter)
    {
        ApplyDefaultDateTimeRange(filter);

        return HasValue(filter, "StartDate")
            && HasValue(filter, "StartTime")
            && HasValue(filter, "EndDate")
            && HasValue(filter, "EndTime");
    }

    public static bool ShouldApply(int queryCount, object filter)
    {
        ApplyDefaultDateTimeRange(filter);
        return HasRequiredDateTime(filter);
    }

    // Set one shared rolling range before a controller invokes its report query.
    public static void ApplyDefaultDateTimeRange(object filter)
    {
        var end = DateTime.Now;
        var start = end.AddHours(-1);

        SetDefaultValue(filter, "StartDate", DateOnly.FromDateTime(start));
        SetDefaultValue(filter, "StartTime", TimeOnly.FromDateTime(start));
        SetDefaultValue(filter, "EndDate", DateOnly.FromDateTime(end));
        SetDefaultValue(filter, "EndTime", TimeOnly.FromDateTime(end));
    }

    private static bool HasValue(object source, string propertyName)
    {
        var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        return value != null;
    }

    private static void SetDefaultValue(object source, string propertyName, object defaultValue)
    {
        var property = source.GetType().GetProperty(propertyName);

        if (property == null || property.GetValue(source) != null)
        {
            return;
        }

        property.SetValue(source, defaultValue);
    }
}
