using FitBook_App.Data;
using FitBook_App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBook_App.Controllers
{
    [Route("api/lookups")]
    [ApiController]
    public class LookupsController : ControllerBase
    {
        private readonly AppDbContext _appDbContext;

        public LookupsController(AppDbContext appDbContext)
        {
            this._appDbContext = appDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetLookups([FromQuery] string type)
        {
            var lookups = await this._appDbContext.Lookups
                .Where(lookup => lookup.Type == type)
                .OrderBy(lookup => lookup.Id)
                .Select(lookup => new LookupResponse
                {
                    Id = lookup.Id,
                    Value = lookup.Value,
                })
                .ToListAsync();

            return Ok(lookups);
        }
    }
}
