using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers.Report;

[Route("[controller]")]
public class DowntimeReportController : Controller
{
    private readonly IDowntimeReportService reportService;

    public DowntimeReportController(IDowntimeReportService reportService)
    {
        this.reportService = reportService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] DowntimeReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = Request.Query.Count > 0;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        return View(viewModel);
    }

    [HttpGet("ExportExcel")]
    public async Task<IActionResult> ExportExcel([FromQuery] DowntimeReportFilter filter, CancellationToken cancellationToken)
    {
        filter.IsApplied = true;
        var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
        var html = new System.Text.StringBuilder();
        html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
        html.AppendLine("<tr><th>Line Name</th><th>Chip pickup error (count_cperr)</th><th>Chip pickup error (time_cperr)</th><th>Chip recog error (count_crerr)</th><th>Chip recog error (time_crerr)</th><th>Single error stop (count_scestop)</th><th>Single error stop (time_scestop)</th><th>Trouble stop (count_trbl)</th><th>Trouble stop (time_trbl)</th><th>Part exhaust stop (count_pwait)</th><th>Part exhaust stop (time_pwait)</th></tr>");

        foreach (var row in viewModel.Rows)
        {
            html.Append("<tr>");
            html.Append($"<td>{Encode(row.LineName)}</td>");
            html.Append($"<td>{row.ChipPickupErrorCount}</td>");
            html.Append($"<td>{row.ChipPickupErrorTime:N2}</td>");
            html.Append($"<td>{row.ChipRecogErrorCount}</td>");
            html.Append($"<td>{row.ChipRecogErrorTime:N2}</td>");
            html.Append($"<td>{row.SingleErrorStopCount}</td>");
            html.Append($"<td>{row.SingleErrorStopTime:N2}</td>");
            html.Append($"<td>{row.TroubleStopCount}</td>");
            html.Append($"<td>{row.TroubleStopTime:N2}</td>");
            html.Append($"<td>{row.PartExhaustStopCount}</td>");
            html.Append($"<td>{row.PartExhaustStopTime:N2}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table></body></html>");

        return File(
            System.Text.Encoding.UTF8.GetBytes(html.ToString()),
            "application/vnd.ms-excel",
            $"DowntimeReport_{DateTime.Now:yyyyMMddHHmmss}.xls");
    }

    private static string Encode(string value)
    {
        return System.Net.WebUtility.HtmlEncode(value);
    }
}
