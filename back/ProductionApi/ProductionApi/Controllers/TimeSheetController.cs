using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeSheetController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public TimeSheetController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/TimeSheet
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeSheet>>> GetTimeSheets()
        {
            return await _context.TimeSheet.Include(t => t.Person).ToListAsync();
        }

        // GET: api/TimeSheet/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeSheet>> GetTimeSheet(int id)
        {
            var timeSheet = await _context.TimeSheet.Include(t => t.Person).FirstOrDefaultAsync(t => t.TimeSheetID == id);

            if (timeSheet == null)
            {
                return NotFound();
            }

            return timeSheet;
        }

        // GET: api/TimeSheet/ByPerson/5
        [HttpGet("ByPerson/{personId}")]
        public async Task<ActionResult<IEnumerable<TimeSheet>>> GetTimeSheetsByPerson(int personId)
        {
            var timeSheets = await _context.TimeSheet
                .Where(t => t.PersonID == personId)
                .Include(t => t.Person)
                .ToListAsync();

            return timeSheets;
        }

        // GET: api/TimeSheet/ByPerson/5/2026-01-01/2026-01-31
        [HttpGet("ByPerson/{personId}/{startDate}/{endDate}")]
        public async Task<ActionResult<IEnumerable<TimeSheet>>> GetTimeSheetsByPersonAndDateRange(int personId, DateTime startDate, DateTime endDate)
        {
            var timeSheets = await _context.TimeSheet
                .Where(t => t.PersonID == personId && t.WorkDate >= startDate && t.WorkDate <= endDate)
                .Include(t => t.Person)
                .OrderBy(t => t.WorkDate)
                .ToListAsync();

            return timeSheets;
        }

        // POST: api/TimeSheet
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<TimeSheet>> CreateTimeSheet(TimeSheet timeSheet)
        {
            // Проверка существования Person
            if (!_context.People.Any(p => p.PersonID == timeSheet.PersonID))
            {
                return BadRequest("PersonID не существует");
            }

            _context.TimeSheet.Add(timeSheet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTimeSheet), new { id = timeSheet.TimeSheetID }, timeSheet);
        }

        // PUT: api/TimeSheet/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeSheet(int id, TimeSheet timeSheet)
        {
            if (id != timeSheet.TimeSheetID)
            {
                return BadRequest();
            }

            _context.Entry(timeSheet).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TimeSheetExists(id))
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

        // DELETE: api/TimeSheet/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeSheet(int id)
        {
            var timeSheet = await _context.TimeSheet.FindAsync(id);
            if (timeSheet == null)
            {
                return NotFound();
            }

            _context.TimeSheet.Remove(timeSheet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TimeSheetExists(int id)
        {
            return _context.TimeSheet.Any(e => e.TimeSheetID == id);
        }
    }
}
