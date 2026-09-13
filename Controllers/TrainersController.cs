using FitBook_App.Models;
using FitBook_App.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitBook_App.Controllers
{
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
            try
            {
                return Ok(await _trainerService.GetAllAsync());
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTrainerById(int id)
        {
            try
            {
                var trainer = await _trainerService.GetByIdAsync(id);
                if (trainer == null)
                {
                    return NotFound();
                }

                return Ok(trainer);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetTrainerByNameAsync([FromRoute] string name)
        {
            var result = await _trainerService.GetByNameAsync(name);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrainer([FromBody] TrainerRequest request)
        {
            try
            {
                var trainer = await _trainerService.CreateAsync(request);
                return CreatedAtAction(nameof(GetTrainerById), new { id = trainer.Id }, trainer);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrainer(int id, [FromBody] TrainerRequest request)
        {
            try
            {
                var trainer = await _trainerService.UpdateAsync(id, request);
                if (trainer == null)
                {
                    return NotFound();
                }

                return Ok(trainer);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrainer(int id)
        {
            try
            {
                var result = await _trainerService.DeleteAsync(id);
                if (result == "NotFound")
                {
                    return NotFound();
                }

                if (result == "InUse")
                {
                    return Conflict("This trainer is assigned to one or more classes.");
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }
    }
}
