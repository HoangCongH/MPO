using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers;

[Route("[controller]")]
public class BoardCountController : Controller
{
    private readonly IBoardCountChartService boardCountChartService;

    public BoardCountController(IBoardCountChartService boardCountChartService)
    {
        this.boardCountChartService = boardCountChartService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        var hasSubmittedFilter = Request.Query.Count > 0;
        filter.IsApplied = ReportFilterGuard.ShouldApply(Request.Query.Count, filter);
        var viewModel = await boardCountChartService.GetChartAsync(filter, cancellationToken);
        if (hasSubmittedFilter && !filter.IsApplied)
        {
            viewModel.ErrorMessage = ReportFilterGuard.RequiredDateTimeMessage;
        }

        return View(viewModel);
    }
}
