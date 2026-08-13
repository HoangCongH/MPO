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
    [HttpPost("Index")]
    public async Task<IActionResult> Index([FromForm] BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        var hasSubmittedFilter = HttpContext.Request.Method == HttpMethods.Post;

        // Board Count has Start/End time controls rather than a shift selector.
        filter.Shift = 3;
        filter.IsApplied = ReportFilterGuard.ShouldApply(Request.Query.Count, filter);
        var viewModel = await boardCountChartService.GetChartAsync(filter, cancellationToken);
        if (hasSubmittedFilter && !filter.IsApplied)
        {
            viewModel.ErrorMessage = ReportFilterGuard.RequiredDateTimeMessage;
        }

        return View(viewModel);
    }
}
