using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers.Report;

[Route("[controller]")]
public class CycleTimeReportController : Controller
{
    private readonly ICycleTimeReportService reportService;

    public CycleTimeReportController(ICycleTimeReportService reportService)
    {
        this.reportService = reportService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] CycleTimeReportFilter filter, CancellationToken cancellationToken)
    {
        var hasSubmittedFilter = Request.Query.Count > 0;
        filter.IsApplied = ReportFilterGuard.ShouldApply(Request.Query.Count, filter);
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        if (hasSubmittedFilter && !filter.IsApplied)
        {
            viewModel.ErrorMessage = ReportFilterGuard.RequiredDateTimeMessage;
        }

        return View(viewModel);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel([FromQuery] CycleTimeReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = ReportFilterGuard.HasRequiredDateTime(filter);
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        html.Append("<tr><th>Line Name</th><th>Model name</th><th>Group name</th>");
        if (viewModel.ShowCycleTime1)
        {
            html.Append("<th>Cycle time 1</th>");
        }

        if (viewModel.ShowCycleTime2)
        {
            html.Append("<th>Cycle time 2</th>");
        }

        if (viewModel.ShowCycleTime3)
        {
            html.Append("<th>Cycle time 3</th>");
        }

        html.AppendLine("</tr>");

        foreach (var row in viewModel.Rows)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(row.LineName)}</td>");
            html.Append($"<td>{Encode(row.ModelName)}</td>");
            html.Append($"<td>{Encode(row.GroupName)}</td>");
            if (viewModel.ShowCycleTime1)
            {
                html.Append($"<td>{FormatCycleTime(row.CycleTime1)}</td>");
            }

            if (viewModel.ShowCycleTime2)
            {
                html.Append($"<td>{FormatCycleTime(row.CycleTime2)}</td>");
            }

            if (viewModel.ShowCycleTime3)
            {
                html.Append($"<td>{FormatCycleTime(row.CycleTime3)}</td>");
            }

            html.AppendLine("</tr>");
        }

        html.AppendLine("</table></body></html>");

        return File(
            System.Text.Encoding.UTF8.GetBytes(html.ToString()),
            "application/vnd.ms-excel",
            $"CycleTimeReport_{DateTime.Now:yyyyMMddHHmmss}.xls");
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }

    private static string FormatCycleTime(decimal? value)
    {
        return value.HasValue
            ? value.Value.ToString("N2")
            : string.Empty;
    }
}
