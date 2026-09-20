using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Authorize]
[Route("api/classes")]
[ApiController]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses([FromQuery] int? trainerUserId)
    {
        return Ok(await _classService.GetAllAsync(trainerUserId));
    }

    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] GymClassRequest request)
    {
        return Ok(await _classService.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] GymClassRequest request)
    {
        var gymClass = await _classService.UpdateAsync(id, request);
        if (gymClass == null)
        {
            return NotFound();
        }

        return Ok(gymClass);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClass(int id)
    {
        var result = await _classService.DeleteAsync(id);
        if (result == "NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }
}
