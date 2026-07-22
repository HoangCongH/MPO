using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers.Report;

[Route("[controller]")]
public class TotalPickupPlacementReportController : Controller
{
    private readonly ITotalPickupPlacementReportService reportService;

    public TotalPickupPlacementReportController(ITotalPickupPlacementReportService reportService)
    {
        this.reportService = reportService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] TotalPickupPlacementReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = Request.Query.Count > 0;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        return View(viewModel);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel([FromQuery] TotalPickupPlacementReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = true;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        html.AppendLine("<tr><th>Line</th><th>Total pickup</th><th>Total placement</th><th>PPM</th></tr>");

        foreach (var row in viewModel.Rows)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(row.LineName)}</td>");
            html.Append($"<td>{row.TotalPickup}</td>");
            html.Append($"<td>{row.TotalPlacement}</td>");
            html.Append($"<td>{row.Ppm:N2}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table></body></html>");

        return File(
            System.Text.Encoding.UTF8.GetBytes(html.ToString()),
            "application/vnd.ms-excel",
            $"TotalPickupPlacementReport_{DateTime.Now:yyyyMMddHHmmss}.xls");
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }
}
