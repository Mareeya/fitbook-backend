using System.ComponentModel.DataAnnotations;

namespace FitBook_App.Helpers;

public sealed class SafeNameAttribute : RegularExpressionAttribute
{
    public SafeNameAttribute()
        : base(@"^[A-Za-z]+(?:[ '\-][A-Za-z]+)*$")
    {
        ErrorMessage = "Use letters only. Spaces, apostrophes, and hyphens are allowed.";
    }
}

public sealed class SafeLabelAttribute : RegularExpressionAttribute
{
    public SafeLabelAttribute()
        : base(@"^[A-Za-z0-9]+(?:[ '\-][A-Za-z0-9]+)*$")
    {
        ErrorMessage = "Use letters and numbers only. Spaces, apostrophes, and hyphens are allowed.";
    }
}
