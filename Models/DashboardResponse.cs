namespace FitBook_App.Models;

public class DashboardSummaryResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int SessionsHeld { get; set; }
    public int Booked { get; set; }
    public int Attended { get; set; }
    public int NoShows { get; set; }
    public int CapacityOffered { get; set; }

    // Percentages, rounded to whole numbers.
    public int AttendanceRate { get; set; }
    public int NoShowRate { get; set; }
    public int Utilization { get; set; }
    public int ActiveMembers { get; set; }
}

public class TodaySessionResponse
{
    public int SessionId { get; set; }
    public DateTime StartAt { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int SeatsTaken { get; set; }
    public int CheckedInCount { get; set; }
    public int FillRate { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsUnderFilled { get; set; }
    public bool IsFull { get; set; }
}
