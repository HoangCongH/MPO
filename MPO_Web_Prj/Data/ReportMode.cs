namespace MPO_Web_Prj.Data;

public static class ReportMode
{
    public const string CookieName = "mpo-report-mode";

    public static bool IsPro(HttpContext? context) =>
        string.Equals(context?.Request.Cookies[CookieName], "pro", StringComparison.Ordinal);
}
