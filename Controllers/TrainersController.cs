using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Controllers
{
    [Route("api/trainers")]
    [ApiController]
    public class TrainersController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public TrainersController(AppDbContext appDbContext)
        {
            this._appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTrainers()
        {
            try
            {
                var trainers = await this._appDbContext.Trainers.ToListAsync();

                var result = new List<TrainerResponse>();
                foreach (var trainer in trainers)
                {
                    result.Add(ToTrainerResponse(trainer));
                }

                return Ok(result);
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
                var trainer = await this._appDbContext.Trainers.FindAsync(id);
                if (trainer == null)
                {
                    return NotFound();
                }

                return Ok(ToTrainerResponse(trainer));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetTrainerByNameAsync([FromRoute] string name)
        {
            var result = await _appDbContext.Trainers.Where(x => x.Name == name).FirstOrDefaultAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrainer([FromBody] TrainerRequest request)
        {
            try
            {
                var trainer = new Trainer
                {
                    Name = request.Name.Trim(),
                    Specialty = request.Specialty.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                this._appDbContext.Trainers.Add(trainer);
                await this._appDbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTrainerById), new { id = trainer.Id }, ToTrainerResponse(trainer));
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
                var trainer = await this._appDbContext.Trainers.FindAsync(id);
                if (trainer == null)
                {
                    return NotFound();
                }

                trainer.Name = request.Name.Trim();
                trainer.Specialty = request.Specialty.Trim();
                trainer.UpdatedAt = DateTime.UtcNow;

                await this._appDbContext.SaveChangesAsync();
                return Ok(ToTrainerResponse(trainer));
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
                var trainer = await this._appDbContext.Trainers.FindAsync(id);
                if (trainer == null)
                {
                    return NotFound();
                }

                this._appDbContext.Trainers.Remove(trainer);
                await this._appDbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("This trainer is assigned to one or more classes.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        private TrainerResponse ToTrainerResponse(Trainer trainer)
        {
            return new TrainerResponse
            {
                Id = trainer.Id,
                Name = trainer.Name,
                Specialty = trainer.Specialty,
                CreatedAt = trainer.CreatedAt,
                UpdatedAt = trainer.UpdatedAt
            };
        }
    }
}
