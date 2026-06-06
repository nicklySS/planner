using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentTimeSheetController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public EquipmentTimeSheetController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/EquipmentTimeSheet
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentTimeSheet>>> GetEquipmentTimeSheets()
        {
            return await _context.EquipmentTimeSheet.Include(e => e.Equipment).ToListAsync();
        }

        // GET: api/EquipmentTimeSheet/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentTimeSheet>> GetEquipmentTimeSheet(int id)
        {
            var equipmentTimeSheet = await _context.EquipmentTimeSheet
                .Include(e => e.Equipment)
                .FirstOrDefaultAsync(e => e.EquipmentTimeSheetID == id);

            if (equipmentTimeSheet == null)
            {
                return NotFound();
            }

            return equipmentTimeSheet;
        }

        // GET: api/EquipmentTimeSheet/ByEquipment/5
        [HttpGet("ByEquipment/{equipmentId}")]
        public async Task<ActionResult<IEnumerable<EquipmentTimeSheet>>> GetEquipmentTimeSheetsByEquipment(int equipmentId)
        {
            var timeSheets = await _context.EquipmentTimeSheet
                .Where(e => e.EquipmentID == equipmentId)
                .Include(e => e.Equipment)
                .OrderBy(e => e.WorkDate)
                .ToListAsync();

            return timeSheets;
        }

        // GET: api/EquipmentTimeSheet/ByEquipment/5/2026-01-01/2026-01-31
        [HttpGet("ByEquipment/{equipmentId}/{startDate}/{endDate}")]
        public async Task<ActionResult<IEnumerable<EquipmentTimeSheet>>> GetEquipmentTimeSheetsByEquipmentAndDateRange(
            int equipmentId, DateTime startDate, DateTime endDate)
        {
            var timeSheets = await _context.EquipmentTimeSheet
                .Where(e => e.EquipmentID == equipmentId && e.WorkDate >= startDate && e.WorkDate <= endDate)
                .Include(e => e.Equipment)
                .OrderBy(e => e.WorkDate)
                .ToListAsync();

            return timeSheets;
        }

        // GET: api/EquipmentTimeSheet/ByDate/2026-01-01
        [HttpGet("ByDate/{workDate}")]
        public async Task<ActionResult<IEnumerable<EquipmentTimeSheet>>> GetEquipmentTimeSheetsByDate(DateTime workDate)
        {
            var timeSheets = await _context.EquipmentTimeSheet
                .Where(e => e.WorkDate == workDate)
                .Include(e => e.Equipment)
                .OrderBy(e => e.EquipmentID)
                .ToListAsync();

            return timeSheets;
        }

        // GET: api/EquipmentTimeSheet/ByDayType/Work
        [HttpGet("ByDayType/{dayType}")]
        public async Task<ActionResult<IEnumerable<EquipmentTimeSheet>>> GetEquipmentTimeSheetsByDayType(string dayType)
        {
            var timeSheets = await _context.EquipmentTimeSheet
                .Where(e => e.DayType == dayType)
                .Include(e => e.Equipment)
                .ToListAsync();

            return timeSheets;
        }

        // POST: api/EquipmentTimeSheet
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<EquipmentTimeSheet>> CreateEquipmentTimeSheet(EquipmentTimeSheet equipmentTimeSheet)
        {
            // Проверка существования Equipment
            if (!_context.Equipment.Any(eq => eq.EquipmentID == equipmentTimeSheet.EquipmentID))
            {
                return BadRequest("EquipmentID не существует");
            }

            equipmentTimeSheet.CreatedAt = DateTime.UtcNow;
            _context.EquipmentTimeSheet.Add(equipmentTimeSheet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipmentTimeSheet), new { id = equipmentTimeSheet.EquipmentTimeSheetID }, equipmentTimeSheet);
        }

        // PUT: api/EquipmentTimeSheet/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipmentTimeSheet(int id, EquipmentTimeSheet equipmentTimeSheet)
        {
            if (id != equipmentTimeSheet.EquipmentTimeSheetID)
            {
                return BadRequest();
            }

            equipmentTimeSheet.ModifiedAt = DateTime.UtcNow;
            _context.Entry(equipmentTimeSheet).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentTimeSheetExists(id))
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

        // DELETE: api/EquipmentTimeSheet/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipmentTimeSheet(int id)
        {
            var equipmentTimeSheet = await _context.EquipmentTimeSheet.FindAsync(id);
            if (equipmentTimeSheet == null)
            {
                return NotFound();
            }

            _context.EquipmentTimeSheet.Remove(equipmentTimeSheet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EquipmentTimeSheetExists(int id)
        {
            return _context.EquipmentTimeSheet.Any(e => e.EquipmentTimeSheetID == id);
        }
    }
}
