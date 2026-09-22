namespace FitBook_App.Configuration;

public class AttendanceOptions
{
    public const string SectionName = "Attendance";

    // How long after a session starts before anything still Booked counts as a no-show.
    // Sessions carry no duration yet, so this stands in for "the session has ended" -
    // once Sessions gain a duration this should become StartAt + duration + a short grace.
    public int NoShowGraceMinutes { get; set; } = 120;

    // How often the sweep runs.
    public int SweepIntervalMinutes { get; set; } = 15;
}
