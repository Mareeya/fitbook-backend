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
        public async Task<IActionResult> GetAllClasses([FromQuery] int? trainerUserId)
        {
            var query = this._appDbContext.Classes
                .Include(gymClass => gymClass.Category)
                .Include(gymClass => gymClass.Trainer)
                .Include(gymClass => gymClass.Sessions)
                    .ThenInclude(session => session.Bookings)
                .AsQueryable();

            if (trainerUserId.HasValue)
            {
                query = query.Where(gymClass => gymClass.Trainer.UserId == trainerUserId.Value);
            }

            var classes = await query.ToListAsync();
            return Ok(classes.Select(ToClassResponse).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] GymClassRequest request)
        {
            var name = request.Name.Trim();
            var nameTaken = await this._appDbContext.Classes.AnyAsync(item => item.Name == name);
            if (nameTaken)
            {
                return Conflict("A class with this name already exists.");
            }

            var gymClass = new GymClass
            {
                Name = name,
                CategoryId = request.CategoryId,
                TrainerId = request.TrainerId,
                InitCapacity = request.InitCapacity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            this._appDbContext.Classes.Add(gymClass);
            await this._appDbContext.SaveChangesAsync();

            return Ok(await this.GetClassResponse(gymClass.Id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] GymClassRequest request)
        {
            var gymClass = await this._appDbContext.Classes.FindAsync(id);
            if (gymClass == null)
            {
                return NotFound();
            }

            var name = request.Name.Trim();
            var nameTaken = await this._appDbContext.Classes.AnyAsync(item => item.Name == name && item.Id != id);
            if (nameTaken)
            {
                return Conflict("A class with this name already exists.");
            }

            gymClass.Name = name;
            gymClass.CategoryId = request.CategoryId;
            gymClass.TrainerId = request.TrainerId;
            gymClass.InitCapacity = request.InitCapacity;
            gymClass.UpdatedAt = DateTime.UtcNow;

            await this._appDbContext.SaveChangesAsync();
            return Ok(await this.GetClassResponse(gymClass.Id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
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

        private async Task<GymClassResponse> GetClassResponse(int id)
        {
            var gymClass = await this._appDbContext.Classes
                .Include(item => item.Category)
                .Include(item => item.Trainer)
                .Include(item => item.Sessions)
                    .ThenInclude(session => session.Bookings)
                .FirstAsync(item => item.Id == id);

            return ToClassResponse(gymClass);
        }

        private static GymClassResponse ToClassResponse(GymClass gymClass)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var monthSessions = gymClass.Sessions
                .Where(session => session.StartAt >= monthStart && session.StartAt < monthEnd)
                .ToList();

            var averageFillRate = 0;
            if (monthSessions.Count > 0)
            {
                averageFillRate = (int)Math.Round(monthSessions.Average(session =>
                    session.Capacity <= 0 ? 0 : (double)session.Bookings.Count / session.Capacity * 100));
            }

            return new GymClassResponse
            {
                Id = gymClass.Id,
                Name = gymClass.Name,
                CategoryId = gymClass.CategoryId,
                CategoryName = gymClass.Category.Value,
                TrainerId = gymClass.TrainerId,
                TrainerName = gymClass.Trainer.Name,
                InitCapacity = gymClass.InitCapacity,
                SessionsThisMonth = monthSessions.Count,
                AverageFillRate = averageFillRate,
                CreatedAt = gymClass.CreatedAt,
                UpdatedAt = gymClass.UpdatedAt
            };
        }
    }
}
