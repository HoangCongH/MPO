using Microsoft.AspNetCore.Mvc;

namespace MPO_Web_Prj.Controllers.Report
{
    [Route("[controller]")]
    public class ProductionReportController : Controller
    {
        private readonly MPO_Web_Prj.Services.Reports.IProductionReportService productionReportService;

        public ProductionReportController(MPO_Web_Prj.Services.Reports.IProductionReportService productionReportService)
        {
            this.productionReportService = productionReportService;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        [HttpPost("Index")]
        public async Task<IActionResult> Index([FromForm] MPO_Web_Prj.Models.Report.ProductionReportFilter filter, CancellationToken cancellationToken)
        {
            var hasSubmittedFilter = HttpContext.Request.Method == HttpMethods.Post;
            filter.IsApplied = MPO_Web_Prj.Services.Reports.ReportFilterGuard.ShouldApply(Request.Query.Count, filter);
            var viewModel = await productionReportService.GetReportAsync(filter, cancellationToken);
            if (hasSubmittedFilter && !filter.IsApplied)
            {
                viewModel.ErrorMessage = MPO_Web_Prj.Services.Reports.ReportFilterGuard.RequiredDateTimeMessage;
            }

            return View(viewModel);
        }

        [HttpPost("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromForm] MPO_Web_Prj.Models.Report.ProductionReportFilter filter, CancellationToken cancellationToken)
        {
            filter.ExportAll = true;
            filter.IsApplied = MPO_Web_Prj.Services.Reports.ReportFilterGuard.HasRequiredDateTime(filter);
            var viewModel = await productionReportService.GetReportAsync(filter, cancellationToken);
            var html = new System.Text.StringBuilder();
            html.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
            html.AppendLine("<tr><th>Line Name</th><th>Lane</th><th>Model name</th><th>Group_name</th><th>Produced quantity (Panel)</th><th>Produced quantity (Pattern)</th><th>Start Time</th><th>End Time</th></tr>");

            foreach (var row in viewModel.Rows)
            {
                html.Append("<tr>");
                html.Append($"<td>{Encode(row.LineName)}</td>");
                html.Append($"<td>{Encode(row.Lane)}</td>");
                html.Append($"<td>{Encode(row.ModelName)}</td>");
                html.Append($"<td>{Encode(row.GroupName)}</td>");
                html.Append($"<td>{row.ProducedQuantityPanel}</td>");
                html.Append($"<td>{row.ProducedQuantityPattern}</td>");
                html.Append($"<td>{Encode(row.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty)}</td>");
                html.Append($"<td>{Encode(row.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty)}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table></body></html>");

            return File(
                System.Text.Encoding.UTF8.GetBytes(html.ToString()),
                "application/vnd.ms-excel",
                $"ProductionReport_{DateTime.Now:yyyyMMddHHmmss}.xls");
        }

        [HttpPost("Batch")]
        public async Task<IActionResult> Batch([FromForm] MPO_Web_Prj.Models.Report.ProductionReportFilter filter, [FromForm] int offset, CancellationToken cancellationToken)
        {
            filter.IsApplied = MPO_Web_Prj.Services.Reports.ReportFilterGuard.HasRequiredDateTime(filter);
            if (!filter.IsApplied) return BadRequest(new { error = MPO_Web_Prj.Services.Reports.ReportFilterGuard.RequiredDateTimeMessage });
            return Json(await productionReportService.GetBatchAsync(filter, offset, MPO_Web_Prj.Models.Report.ReportPagination.DefaultPageSize, cancellationToken));
        }

        [HttpPost("Options")]
        public async Task<IActionResult> Options(
            [FromQuery] string? search,
            [FromQuery] int limit,
            [FromForm] MPO_Web_Prj.Models.Report.ProductionReportFilter filter,
            CancellationToken cancellationToken)
        {
            filter.IsApplied = MPO_Web_Prj.Services.Reports.ReportFilterGuard.HasRequiredDateTime(filter);
            return Json(await productionReportService.GetFilterOptionsAsync(filter, search, limit, cancellationToken));
        }

        private static string Encode(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value);
        }
    }
}
