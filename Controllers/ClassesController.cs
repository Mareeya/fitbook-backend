using FitBook_App.Data;
using FitBook_App.Domain;
using FitBook_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Controllers
{
    [Route("api/classes")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public ClassesController(AppDbContext appDbContext)
        {
            this._appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClasses()
        {
            try
            {
                var classes = await this._appDbContext.Classes
                    .Include(gymClass => gymClass.Category)
                    .Include(gymClass => gymClass.Trainer)
                    .ToListAsync();

                var result = new List<GymClassResponse>();
                foreach (var gymClass in classes)
                {
                    result.Add(ToClassResponse(gymClass));
                }

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClassById(int id)
        {
            try
            {
                var gymClass = await this._appDbContext.Classes
                    .Include(item => item.Category)
                    .Include(item => item.Trainer)
                    .FirstOrDefaultAsync(item => item.Id == id);

                if (gymClass == null)
                {
                    return NotFound();
                }

                return Ok(ToClassResponse(gymClass));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] GymClassRequest request)
        {
            try
            {
                var category = await this._appDbContext.Lookups.FindAsync(request.CategoryId);
                if (category == null)
                {
                    return BadRequest("Category does not exist.");
                }

                var trainer = await this._appDbContext.Trainers.FindAsync(request.TrainerId);
                if (trainer == null)
                {
                    return BadRequest("Trainer does not exist.");
                }

                var gymClass = new GymClass
                {
                    Name = request.Name.Trim(),
                    CategoryId = request.CategoryId,
                    TrainerId = request.TrainerId,
                    InitCapacity = request.InitCapacity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                this._appDbContext.Classes.Add(gymClass);
                await this._appDbContext.SaveChangesAsync();

                gymClass.Category = category;
                gymClass.Trainer = trainer;
                return CreatedAtAction(nameof(GetClassById), new { id = gymClass.Id }, ToClassResponse(gymClass));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] GymClassRequest request)
        {
            try
            {
                var gymClass = await this._appDbContext.Classes.FindAsync(id);
                if (gymClass == null)
                {
                    return NotFound();
                }

                var category = await this._appDbContext.Lookups.FindAsync(request.CategoryId);
                if (category == null)
                {
                    return BadRequest("Category does not exist.");
                }

                var trainer = await this._appDbContext.Trainers.FindAsync(request.TrainerId);
                if (trainer == null)
                {
                    return BadRequest("Trainer does not exist.");
                }

                gymClass.Name = request.Name.Trim();
                gymClass.CategoryId = request.CategoryId;
                gymClass.TrainerId = request.TrainerId;
                gymClass.InitCapacity = request.InitCapacity;
                gymClass.UpdatedAt = DateTime.UtcNow;

                await this._appDbContext.SaveChangesAsync();

                gymClass.Category = category;
                gymClass.Trainer = trainer;
                return Ok(ToClassResponse(gymClass));
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            try
            {
                var gymClass = await this._appDbContext.Classes.FindAsync(id);
                if (gymClass == null)
                {
                    return NotFound();
                }

                this._appDbContext.Classes.Remove(gymClass);
                await this._appDbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict("This class has sessions, so it cannot be deleted.");
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        private GymClassResponse ToClassResponse(GymClass gymClass)
        {
            return new GymClassResponse
            {
                Id = gymClass.Id,
                Name = gymClass.Name,
                CategoryId = gymClass.CategoryId,
                CategoryName = gymClass.Category.Value,
                TrainerId = gymClass.TrainerId,
                TrainerName = gymClass.Trainer.Name,
                InitCapacity = gymClass.InitCapacity,
                CreatedAt = gymClass.CreatedAt,
                UpdatedAt = gymClass.UpdatedAt
            };
        }
    }
}
