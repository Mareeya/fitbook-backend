using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize(Roles = RoleNames.ManageOrTrainer)]
[Route("api")]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ISessionService _sessionService;

    public AttendanceController(IAttendanceService attendanceService, ISessionService sessionService)
    {
        _attendanceService = attendanceService;
        _sessionService = sessionService;
    }

    [HttpPost("sessions/{sessionId:int}/attendance")]
    public async Task<IActionResult> MarkAttendance(int sessionId, [FromBody] AttendanceMarkRequest request)
    {
        await EnsureTrainerOwnsSessionAsync(sessionId);

        var roster = await _attendanceService.MarkAsync(sessionId, request, User.GetUserId());
        if (roster == null)
        {
            return NotFound();
        }

        return Ok(roster);
    }

    [HttpPost("sessions/{sessionId:int}/walk-in")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> CheckInWalkIn(int sessionId, [FromBody] WalkInRequest request)
    {
        return Ok(await _attendanceService.WalkInAsync(sessionId, request.UserId, User.GetUserId()));
    }

    [HttpPatch("bookings/{bookingId:int}/attendance")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> UpdateAttendance(int bookingId, [FromBody] AttendanceUpdateRequest request)
    {
        var booking = await _attendanceService.UpdateAsync(bookingId, request.Status, User.GetUserId());
        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }

    // Trainers may mark their own sessions only. Scoped from the token, not a query parameter.
    private async Task EnsureTrainerOwnsSessionAsync(int sessionId)
    {
        if (User.CanManage())
        {
            return;
        }

        var ownsSession = await _sessionService.IsTrainerForSessionAsync(sessionId, User.GetUserId());
        if (!ownsSession)
        {
            throw new AppException(StatusCodes.Status403Forbidden, "You can only mark your own sessions.");
        }
    }
}
