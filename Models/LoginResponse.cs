namespace FitBook_App.Models;

public class LoginResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte Role { get; set; }
    public string Token { get; set; } = string.Empty;
}
