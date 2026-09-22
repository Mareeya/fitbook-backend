using System.ComponentModel.DataAnnotations;

namespace FitBook_App.Models;

public class SessionRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    // Treated as UTC. Sessions are stored in UTC and converted at the edges.
    [Required]
    public DateTime StartAt { get; set; }

    // Leave null to inherit the class's InitCapacity.
    [Range(1, 200)]
    public int? Capacity { get; set; }
}

public class SessionGenerateRequest
{
    [Range(1, int.MaxValue)]
    public int ClassId { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime UntilAt { get; set; }

    // Every how many days to repeat, e.g. 7 for weekly.
    [Range(1, 90)]
    public int RepeatEveryDays { get; set; } = 7;

    [Range(1, 200)]
    public int? Capacity { get; set; }
}
