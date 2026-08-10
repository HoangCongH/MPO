using Microsoft.AspNetCore.Mvc;
using MPO_Web_Prj.Models.Report;
using MPO_Web_Prj.Services.Reports;

namespace MPO_Web_Prj.Controllers;

[Route("[controller]")]
public class OverallDashboardController : Controller
{
    private readonly IOverallDashboardService overallDashboardService;

    public OverallDashboardController(IOverallDashboardService overallDashboardService)
    {
        this.overallDashboardService = overallDashboardService;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index([FromQuery] BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        var hasSubmittedFilter = Request.Query.Count > 0;
        var hasRequiredShift = filter.Shift is 1 or 2;

        // Overall Dashboard is a single-line view. Keep the Board Count Type
        // selector scoped to the Board Count page.
        filter.IsApplied = hasSubmittedFilter && hasRequiredShift;
        filter.Type = 1;
        filter.Line2 = null;
        filter.Line3 = null;
        filter.Line4 = null;

        var viewModel = await overallDashboardService.GetDashboardAsync(filter, cancellationToken);

        if (hasSubmittedFilter && !hasRequiredShift)
        {
            viewModel.ErrorMessage = "Please select Shift 1 or Shift 2 before applying the filter.";
        }

        return View(viewModel);
    }
}
