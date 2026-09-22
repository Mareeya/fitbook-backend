using System.ComponentModel.DataAnnotations;

namespace FitBook_App.Models;

public class BookingRequest
{
    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }
}

// Staff booking a seat for someone else. The member is named here because
// the caller is not the member - every other path takes the id from the token.
public class StaffBookingRequest
{
    [Range(1, int.MaxValue)]
    public int SessionId { get; set; }

    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}
