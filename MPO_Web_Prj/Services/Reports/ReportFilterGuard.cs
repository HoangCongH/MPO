namespace MPO_Web_Prj.Services.Reports;

public static class ReportFilterGuard
{
    public const string RequiredDateTimeMessage = "Please enter Start date, Start time, End date, and End time before applying the filter.";

    public static bool HasRequiredDateTime(object filter)
    {
        return HasValue(filter, "StartDate")
            && HasValue(filter, "StartTime")
            && HasValue(filter, "EndDate")
            && HasValue(filter, "EndTime");
    }

    public static bool ShouldApply(int queryCount, object filter)
    {
        return queryCount > 0 && HasRequiredDateTime(filter);
    }

    private static bool HasValue(object source, string propertyName)
    {
        var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
        return value != null;
    }
}
