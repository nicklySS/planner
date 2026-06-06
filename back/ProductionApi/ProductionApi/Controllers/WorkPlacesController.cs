using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkPlacesController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public WorkPlacesController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/WorkPlaces
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkPlace>>> GetWorkPlaces()
        {
            return await _context.WorkPlaces
                .Include(wp => wp.ResponsiblePerson)
                .Include(wp => wp.Equipments) // опционально, показать станки на рабочем месте
                .ToListAsync();
        }

        // GET: api/WorkPlaces/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkPlace>> GetWorkPlace(int id)
        {
            var workPlace = await _context.WorkPlaces
                .Include(wp => wp.ResponsiblePerson)
                .Include(wp => wp.Equipments)
                .FirstOrDefaultAsync(wp => wp.WorkPlaceID == id);

            if (workPlace == null)
            {
                return NotFound();
            }

            return workPlace;
        }

        // POST: api/WorkPlaces
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<WorkPlace>> CreateWorkPlace(WorkPlace workPlace)
        {
            _context.WorkPlaces.Add(workPlace);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetWorkPlace), new { id = workPlace.WorkPlaceID }, workPlace);
        }

        // PUT: api/WorkPlaces/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkPlace(int id, WorkPlace workPlace)
        {
            if (id != workPlace.WorkPlaceID)
            {
                return BadRequest();
            }

            _context.Entry(workPlace).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkPlaceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/WorkPlaces/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkPlace(int id)
        {
            var workPlace = await _context.WorkPlaces.FindAsync(id);
            if (workPlace == null)
            {
                return NotFound();
            }

            _context.WorkPlaces.Remove(workPlace);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WorkPlaceExists(int id)
        {
            return _context.WorkPlaces.Any(e => e.WorkPlaceID == id);
        }
    }
}
