namespace FitBook_App.Models;

public class MeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte Role { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // Set when the user is a trainer, so a client can load their sessions without a second lookup.
    public int? TrainerId { get; set; }
}
