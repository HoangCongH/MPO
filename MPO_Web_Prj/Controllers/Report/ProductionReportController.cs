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
        public async Task<IActionResult> Index([FromQuery] MPO_Web_Prj.Models.Report.ProductionReportFilter filter, CancellationToken cancellationToken)
        {
            var viewModel = await productionReportService.GetReportAsync(filter, cancellationToken);
            return View(viewModel);
        }

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel([FromQuery] MPO_Web_Prj.Models.Report.ProductionReportFilter filter, CancellationToken cancellationToken)
        {
            var viewModel = await productionReportService.GetReportAsync(filter, cancellationToken);
            var lines = new List<string>
            {
                "Line Name,Lane,Model name,Group_name,Produced quantity (Panel),Produced quantity (Pattern),Time"
            };

            lines.AddRange(viewModel.Rows.Select(row =>
                string.Join(',',
                    EscapeCsv(row.LineName),
                    EscapeCsv(row.Lane),
                    EscapeCsv(row.ModelName),
                    EscapeCsv(row.GroupName),
                    row.ProducedQuantityPanel,
                    row.ProducedQuantityPattern,
                    EscapeCsv(row.Time?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty))));

            return File(
                System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines)),
                "text/csv",
                $"ProductionReport_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private static string EscapeCsv(string value)
        {
            if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
