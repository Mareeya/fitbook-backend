using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers;

[Route("api/trainers")]
[ApiController]
public class TrainersController : ControllerBase
{
    private readonly ITrainerService _trainerService;

    public TrainersController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTrainers()
    {
        return Ok(await _trainerService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetTrainerById(int id)
    {
        var trainer = await _trainerService.GetByIdAsync(id);
        if (trainer == null)
        {
            return NotFound();
        }

        return Ok(trainer);
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetTrainerByUserId(int userId)
    {
        var trainer = await _trainerService.GetByUserIdAsync(userId);
        if (trainer == null)
        {
            return NotFound();
        }

        return Ok(trainer);
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> GetTrainerByName([FromRoute] string name)
    {
        return Ok(await _trainerService.GetByNameAsync(name));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTrainer([FromBody] TrainerRequest request)
    {
        var trainer = await _trainerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetTrainerById), new { id = trainer.Id }, trainer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrainer(int id, [FromBody] TrainerRequest request)
    {
        var trainer = await _trainerService.UpdateAsync(id, request);
        if (trainer == null)
        {
            return NotFound();
        }

        return Ok(trainer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrainer(int id)
    {
        var result = await _trainerService.DeleteAsync(id);
        if (result == "NotFound")
        {
            return NotFound();
        }

        return NoContent();
    }
}
