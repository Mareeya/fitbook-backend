using System.ComponentModel.DataAnnotations;
using FitBook_App.Helpers;

namespace FitBook_App.Models;

public class RegisterRequest
{
    [Required]
    [MaxLength(120)]
    [SafeName]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^\S+$", ErrorMessage = "Password cannot contain spaces.")]
    public string Password { get; set; } = string.Empty;
}
