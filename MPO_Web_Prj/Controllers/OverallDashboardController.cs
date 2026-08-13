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
        ReportFilterGuard.ApplyDefaultDateTimeRange(filter);

        // This dashboard uses its Time selector (Shift 1, Shift 2, All Day, Last Hour)
        // instead of Start/End time inputs, so keep that selector authoritative.
        filter.StartTime = null;
        filter.EndTime = null;
        var hasRequiredTime = filter.Shift is >= 1 and <= 4;
        var hasRequiredDate = filter.Shift == 4
            || (filter.StartDate.HasValue && filter.EndDate.HasValue);

        // Overall Dashboard is a single-line view. Keep the Board Count Type
        // selector scoped to the Board Count page.
        filter.IsApplied = hasRequiredTime && hasRequiredDate;
        filter.Type = 1;
        filter.Line2 = null;
        filter.Line3 = null;
        filter.Line4 = null;

        var viewModel = await overallDashboardService.GetDashboardAsync(filter, cancellationToken);

        if (hasSubmittedFilter && !hasRequiredTime)
        {
            viewModel.ErrorMessage = "Please select a Time option before applying the filter.";
        }
        else if (hasSubmittedFilter && !hasRequiredDate)
        {
            viewModel.ErrorMessage = "Please select Start and End dates before applying the filter.";
        }

        return View(viewModel);
    }
}
