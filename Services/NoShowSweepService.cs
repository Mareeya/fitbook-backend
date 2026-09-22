using FitBook_App.Configuration;
using FitBook_App.Data;
using FitBook_App.Domain.Enums;
using FitBook_App.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitBook_App.Services;

/// <summary>
/// Closes out sessions nobody marked. Anything still <see cref="AttendanceStatus.Booked"/> once its
/// session has ended becomes a no-show, so the attendance figures measure what happened rather than
/// which sessions staff remembered to mark.
/// </summary>
public class NoShowSweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NoShowSweepService> _logger;
    private readonly AttendanceOptions _options;

    public NoShowSweepService(
        IServiceScopeFactory scopeFactory,
        ILogger<NoShowSweepService> logger,
        IOptions<AttendanceOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.SweepIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation(
            "No-show sweep started. Running every {Interval} minutes, {Grace} minutes after a session starts.",
            interval.TotalMinutes,
            _options.NoShowGraceMinutes);

        // Sweep once on startup, then on the timer, so a restart catches anything missed while down.
        do
        {
            try
            {
                var marked = await SweepAsync(stoppingToken);
                if (marked > 0)
                {
                    _logger.LogInformation("No-show sweep marked {Count} booking(s).", marked);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Never let one bad pass kill the loop - the next tick should try again.
                _logger.LogError(exception, "No-show sweep failed. Retrying on the next tick.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    internal async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Max(0, _options.NoShowGraceMinutes));

        // Set-based: one UPDATE, no entities loaded. MarkedByUserId is deliberately left null -
        // that null is how you tell an automatic no-show from one a person recorded.
        return await db.Bookings
            .Where(booking => booking.AttendanceStatus == AttendanceStatus.Booked
                && booking.Session.StartAt <= cutoff
                && booking.Status.Value != LookupNames.Cancelled
                && booking.Session.Status.Value != LookupNames.Cancelled)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(booking => booking.AttendanceStatus, AttendanceStatus.NoShow)
                    .SetProperty(booking => booking.UpdatedAt, DateTime.UtcNow),
                cancellationToken);
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
