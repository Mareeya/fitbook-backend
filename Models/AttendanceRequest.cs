using System.ComponentModel.DataAnnotations;
using FitBook_App.Domain.Enums;

namespace FitBook_App.Models;

// Staff mark a room, not a person - the whole roster arrives in one call.
public class AttendanceMarkRequest
{
    [Required]
    [MinLength(1)]
    public List<AttendanceMarkItem> Marks { get; set; } = [];
}

public class AttendanceMarkItem
{
    [Range(1, int.MaxValue)]
    public int BookingId { get; set; }

    [Required]
    [EnumDataType(typeof(AttendanceStatus))]
    public AttendanceStatus Status { get; set; }
}

public class AttendanceUpdateRequest
{
    [Required]
    [EnumDataType(typeof(AttendanceStatus))]
    public AttendanceStatus Status { get; set; }
}

public class WalkInRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}
