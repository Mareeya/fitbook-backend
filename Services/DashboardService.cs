using FitBook_App.Data;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using FitBook_App.Models;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Services;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(DateTime from, DateTime to);
    Task<List<TodaySessionResponse>> GetTodayAsync(DateTime? date);
}

public class DashboardService : IDashboardService
{
    // A session below this fill rate is worth flagging to staff.
    private const int UnderFilledThreshold = 40;

    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(DateTime from, DateTime to)
    {
        if (to < from)
        {
            throw new AppException(StatusCodes.Status400BadRequest, "The end date must be after the start date.");
        }

        var fromUtc = SessionService.ToUtc(from);
        var toUtc = SessionService.ToUtc(to);

        var sessions = await _db.Sessions
            .Where(session => session.StartAt >= fromUtc
                && session.StartAt < toUtc
                && session.Status.Value != LookupNames.Cancelled)
            .Select(session => new { session.Capacity, session.SeatsTaken })
            .ToListAsync();

        // Cancelled bookings are excluded - they were never a seat anyone was expected to fill.
        var attendance = await _db.Bookings
            .Where(booking => booking.Session.StartAt >= fromUtc
                && booking.Session.StartAt < toUtc
                && booking.Status.Value != LookupNames.Cancelled)
            .GroupBy(booking => booking.AttendanceStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();

        var booked = attendance.Sum(item => item.Count);
        var attended = attendance
            .Where(item => item.Status == AttendanceStatus.Attended)
            .Sum(item => item.Count);
        var noShows = attendance
            .Where(item => item.Status == AttendanceStatus.NoShow)
            .Sum(item => item.Count);

        var capacityOffered = sessions.Sum(session => session.Capacity);
        var seatsTaken = sessions.Sum(session => session.SeatsTaken);

        var activeSince = DateTime.UtcNow.AddDays(-28);
        var activeMembers = await _db.Bookings
            .Where(booking => booking.AttendanceStatus == AttendanceStatus.Attended
                && booking.Session.StartAt >= activeSince)
            .Select(booking => booking.UserId)
            .Distinct()
            .CountAsync();

        return new DashboardSummaryResponse
        {
            From = fromUtc,
            To = toUtc,
            SessionsHeld = sessions.Count,
            Booked = booked,
            Attended = attended,
            NoShows = noShows,
            CapacityOffered = capacityOffered,
            AttendanceRate = Percentage(attended, booked),
            NoShowRate = Percentage(noShows, booked),
            Utilization = Percentage(seatsTaken, capacityOffered),
            ActiveMembers = activeMembers
        };
    }

    public async Task<List<TodaySessionResponse>> GetTodayAsync(DateTime? date)
    {
        var day = date.HasValue ? SessionService.ToUtc(date.Value).Date : DateTime.UtcNow.Date;
        var dayStart = DateTime.SpecifyKind(day, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var sessions = await _db.Sessions
            .Where(session => session.StartAt >= dayStart && session.StartAt < dayEnd)
            .Include(session => session.Class).ThenInclude(gymClass => gymClass.Trainer)
            .Include(session => session.Status)
            .OrderBy(session => session.StartAt)
            .ToListAsync();

        return sessions.Select(session =>
        {
            var fillRate = Percentage(session.SeatsTaken, session.Capacity);
            return new TodaySessionResponse
            {
                SessionId = session.Id,
                StartAt = DateTime.SpecifyKind(session.StartAt, DateTimeKind.Utc),
                ClassName = session.Class.Name,
                TrainerName = session.Class.Trainer.Name,
                Capacity = session.Capacity,
                SeatsTaken = session.SeatsTaken,
                CheckedInCount = session.CheckedInCount,
                FillRate = fillRate,
                StatusName = session.Status.Value,
                IsUnderFilled = session.Status.Value != LookupNames.Cancelled && fillRate < UnderFilledThreshold,
                IsFull = session.SeatsTaken >= session.Capacity
            };
        }).ToList();
    }

    private static int Percentage(int part, int whole)
    {
        return whole <= 0 ? 0 : (int)Math.Round((double)part / whole * 100);
    }
}
