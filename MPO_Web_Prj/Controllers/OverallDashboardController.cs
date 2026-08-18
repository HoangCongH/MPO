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
    [HttpPost("Index")]
    public async Task<IActionResult> Index([FromForm] BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        var hasSubmittedFilter = HttpContext.Request.Method == HttpMethods.Post;
        if (!hasSubmittedFilter)
        {
            filter.AutoRefresh = true;
            ApplyActiveShift(filter, DateTime.Now);
        }

        var hasRequiredTime = filter.Shift is >= 1 and <= 4;
        var hasRequiredDate = filter.Shift == 4 || (filter.StartDate.HasValue && filter.EndDate.HasValue);
        ConfigureFilter(filter);

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

    [HttpPost("Refresh")]
    public async Task<IActionResult> Refresh([FromForm] BoardCountChartFilter filter, CancellationToken cancellationToken)
    {
        if (filter.AutoRefresh)
        {
            ApplyActiveShift(filter, filter.ClientNow ?? DateTime.Now);
        }

        ConfigureFilter(filter);
        var viewModel = await overallDashboardService.GetDashboardAsync(filter, cancellationToken);

        return Json(new
        {
            charts = viewModel.BoardProduced.Charts.Select(chart => new
            {
                lineName = chart.LineName,
                labels = chart.Labels,
                series = chart.Series.Select(series => new { laneName = series.LaneName, values = series.Values })
            }),
            errorStop = new
            {
                labels = viewModel.ErrorStopTime.Select(slice => slice.Label),
                values = viewModel.ErrorStopTime.Select(slice => slice.Value)
            },
            feeders = viewModel.TopWorstFeeders,
            nozzles = viewModel.TopWorstNozzles,
            filter = new { filter.Shift, filter.StartDate, filter.EndDate, filter.AutoRefresh },
            errorMessage = viewModel.ErrorMessage
        });
    }

    private static void ConfigureFilter(BoardCountChartFilter filter)
    {
        filter.StartTime = null;
        filter.EndTime = null;
        var hasRequiredTime = filter.Shift is >= 1 and <= 4;
        var hasRequiredDate = filter.Shift == 4 || (filter.StartDate.HasValue && filter.EndDate.HasValue);

        filter.IsApplied = hasRequiredTime && hasRequiredDate;
        filter.Type = 1;
        filter.SplitLanes = true;
        filter.Line2 = null;
        filter.Line3 = null;
        filter.Line4 = null;
    }

    private static void ApplyActiveShift(BoardCountChartFilter filter, DateTime now)
    {
        var shift2Start = new TimeSpan(18, 1, 0);
        var isShift2 = now.TimeOfDay >= shift2Start || now.TimeOfDay < TimeSpan.FromHours(6);
        var shiftDate = isShift2 && now.TimeOfDay < TimeSpan.FromHours(6)
            ? DateOnly.FromDateTime(now.AddDays(-1))
            : DateOnly.FromDateTime(now);

        filter.Shift = isShift2 ? 2 : 1;
        filter.StartDate = shiftDate;
        filter.EndDate = shiftDate;
    }
}
