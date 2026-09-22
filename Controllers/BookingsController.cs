using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize]
[Route("api/bookings")]
[ApiController]
public class BookingsController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookingRequest request)
    {
        // The member is whoever holds the token - never a value from the body.
        var booking = await _bookingService.BookAsync(request.SessionId, User.GetUserId(), BookingSource.Online);
        return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
    }

    [HttpPost("for-member")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> BookForMember([FromBody] StaffBookingRequest request)
    {
        var booking = await _bookingService.BookAsync(request.SessionId, request.UserId, BookingSource.StaffBooked);
        return CreatedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var booking = await _bookingService.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        if (!User.CanManage() && booking.UserId != User.GetUserId())
        {
            return Forbid();
        }

        return Ok(booking);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyBookings(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        return Ok(await _bookingService.GetForUserAsync(
            User.GetUserId(), from, to, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize)));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _bookingService.CancelAsync(id, User.GetUserId(), User.CanManage());
        if (booking == null)
        {
            return NotFound();
        }

        return Ok(booking);
    }
}
