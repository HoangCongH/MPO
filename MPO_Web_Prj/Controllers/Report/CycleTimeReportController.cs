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
        filter.IsApplied = Request.Query.Count > 0;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        return View(viewModel);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel([FromQuery] CycleTimeReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = true;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        html.AppendLine("<tr><th>Line Name</th><th>Model name</th><th>Group name</th><th>Cycle time 1</th><th>Cycle time 2</th><th>Cycle time 3</th></tr>");

        foreach (var row in viewModel.Rows)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(row.LineName)}</td>");
            html.Append($"<td>{Encode(row.ModelName)}</td>");
            html.Append($"<td>{Encode(row.GroupName)}</td>");
            html.Append($"<td>{row.CycleTime1}</td>");
            html.Append($"<td>{row.CycleTime2}</td>");
            html.Append($"<td>{row.CycleTime3}</td>");
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
}
