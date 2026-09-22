using FitBook_App.Helpers;
using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize]
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        return Ok(response);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        return Ok(await _authService.RegisterAsync(request));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var me = await _authService.GetMeAsync(User.GetUserId());
        if (me == null)
        {
            // The token is valid but the user is gone - treat it as a dead session.
            return Unauthorized("Please log in again.");
        }

        return Ok(me);
    }
}
