using FitBook_App.Helpers;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize(Roles = RoleNames.Manage)]
[Route("api/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        // Default to the last 7 days so the dashboard opens with something on it.
        var toUtc = to ?? DateTime.UtcNow.Date.AddDays(1);
        var fromUtc = from ?? toUtc.AddDays(-7);

        return Ok(await _dashboardService.GetSummaryAsync(fromUtc, toUtc));
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday([FromQuery] DateTime? date)
    {
        return Ok(await _dashboardService.GetTodayAsync(date));
    }
}
