using System.Security.Claims;
using FitBook_App.Domain.Enums;

namespace FitBook_App.Helpers;

public static class ClaimsPrincipalExtensions
{
    // Never read the caller's identity from a request body - only from the token.
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
        {
            throw new AppException(StatusCodes.Status401Unauthorized, "Please log in again.");
        }

        return userId;
    }

    public static bool IsInRole(this ClaimsPrincipal principal, UserRole role)
    {
        return principal.IsInRole(role.ToString());
    }

    public static bool CanManage(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Admin) || principal.IsInRole(UserRole.Staff);
    }
}
