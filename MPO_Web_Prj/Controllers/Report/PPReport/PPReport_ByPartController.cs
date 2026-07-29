using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers.Report.PPReport
{
    [Route("[controller]")]
    public class PPReport_ByPartController : Controller
    {
        private readonly IPickPlacementByPartService reportService;

        public PPReport_ByPartController(IPickPlacementByPartService reportService)
        {
            this.reportService = reportService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index([FromQuery] PickPlacementByPartFilter filter, CancellationToken cancellationToken)
        {
            var hasSubmittedFilter = Request.Query.Count > 0;
            filter.IsApplied = ReportFilterGuard.ShouldApply(Request.Query.Count, filter);
            var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
            if (hasSubmittedFilter && !filter.IsApplied)
            {
                viewModel.ErrorMessage = ReportFilterGuard.RequiredDateTimeMessage;
            }

            return View("~/Views/Report/PPReport/PPReport_ByPart.cshtml", viewModel);
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] PickPlacementByPartFilter filter, CancellationToken cancellationToken)
        {
            filter.IsApplied = ReportFilterGuard.HasRequiredDateTime(filter);
            var viewModel = await reportService.GetReportAsync(filter, cancellationToken);
            var html = new System.Text.StringBuilder();
            html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
            html.AppendLine("<tr><th>Line Name</th><th>Machine Name</th><th>Stage</th><th>Part Name</th><th>Pickup count</th><th>Placement count</th><th>Pickup miss</th><th>Recog miss</th><th>Height miss</th><th>Drop miss</th><th>Mount miss</th><th>Transfer miss</th><th>Scrap ratio</th></tr>");

            foreach (var row in viewModel.Rows)
            {
                html.Append("<tr>");
                html.Append($"<td>{Encode(row.LineName)}</td>");
                html.Append($"<td>{Encode(row.MachineName)}</td>");
                html.Append($"<td>{Encode(row.Stage)}</td>");
                html.Append($"<td>{Encode(row.PartName)}</td>");
                html.Append($"<td>{row.PickupCount}</td>");
                html.Append($"<td>{row.PlacementCount}</td>");
                html.Append($"<td>{row.PickupMiss}</td>");
                html.Append($"<td>{row.RecogMiss}</td>");
                html.Append($"<td>{row.HeightMiss}</td>");
                html.Append($"<td>{row.DropMiss}</td>");
                html.Append($"<td>{row.MountMiss}</td>");
                html.Append($"<td>{row.TransferMiss}</td>");
                html.Append($"<td>{row.ScrapRatio:N0}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table></body></html>");

            return File(
                System.Text.Encoding.UTF8.GetBytes(html.ToString()),
                "application/vnd.ms-excel",
                $"PickPlacementByPart_{DateTime.Now:yyyyMMddHHmmss}.xls");
        }

        private static string Encode(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value);
        }
    }
}
