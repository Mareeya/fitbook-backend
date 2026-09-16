using System.ComponentModel.DataAnnotations;
using FitBook_App.Helpers;

namespace FitBook_App.Models;

public class TrainerRequest
{
    [Required]
    [MaxLength(120)]
    [SafeName]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    [RegularExpression(@"^$|^\S{8,100}$", ErrorMessage = "Password must be 8-100 characters with no spaces.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    [SafeLabel]
    public string Specialty { get; set; } = string.Empty;
}
