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
        var viewModel = await overallDashboardService.GetDashboardAsync(filter, cancellationToken);
        return View(viewModel);
    }
}
