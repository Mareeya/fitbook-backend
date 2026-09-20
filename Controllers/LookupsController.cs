using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize]
[Route("api/lookups")]
[ApiController]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLookups([FromQuery] string type)
    {
        return Ok(await _lookupService.GetByTypeAsync(type));
    }
}
