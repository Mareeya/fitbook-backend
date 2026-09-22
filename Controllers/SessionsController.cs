using FitBook_App.Helpers;
using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize]
[Route("api/sessions")]
[ApiController]
public class SessionsController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int? classId,
        [FromQuery] int? trainerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (from == default || to == default)
        {
            return BadRequest("Both 'from' and 'to' are required.");
        }

        return Ok(await _sessionService.GetAllAsync(
            from, to, classId, trainerId, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSessionById(int id)
    {
        var session = await _sessionService.GetByIdAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpGet("{id:int}/roster")]
    [Authorize(Roles = RoleNames.ManageOrTrainer)]
    public async Task<IActionResult> GetRoster(int id)
    {
        // The roster carries member names and emails, so a trainer sees only their own sessions.
        if (!User.CanManage() && !await _sessionService.IsTrainerForSessionAsync(id, User.GetUserId()))
        {
            return Forbid();
        }

        var roster = await _sessionService.GetRosterAsync(id);
        if (roster == null)
        {
            return NotFound();
        }

        return Ok(roster);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> CreateSession([FromBody] SessionRequest request)
    {
        var session = await _sessionService.CreateAsync(request);
        return CreatedAtAction(nameof(GetSessionById), new { id = session.Id }, session);
    }

    [HttpPost("generate")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> GenerateSessions([FromBody] SessionGenerateRequest request)
    {
        return Ok(await _sessionService.GenerateAsync(request));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> UpdateSession(int id, [FromBody] SessionRequest request)
    {
        var session = await _sessionService.UpdateAsync(id, request);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = RoleNames.Manage)]
    public async Task<IActionResult> CancelSession(int id)
    {
        var session = await _sessionService.CancelAsync(id);
        if (session == null)
        {
            return NotFound();
        }

        return Ok(session);
    }
}
