using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public EquipmentController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/Equipment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEquipment()
        {
            return await _context.Equipment
                .Select(eq => new
                {
                    eq.EquipmentID,
                    eq.EquipmentName,
                    eq.EquipmentType,
                    eq.WorkPlaceID,
                    WorkPlace = eq.WorkPlace != null ? new
                    {
                        eq.WorkPlace.WorkPlaceID,
                        eq.WorkPlace.Name,
                        eq.WorkPlace.Location
                    } : null,
                    OperationsCount = eq.Operations != null ? eq.Operations.Count : 0,
                    ReconfigurationsCount = eq.ReconfigurationTimes != null ? eq.ReconfigurationTimes.Count : 0,
                    ShiftLogsCount = eq.ShiftWorkLogs != null ? eq.ShiftWorkLogs.Count : 0
                })
                .ToListAsync();
        }

        // GET: api/Equipment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEquipment(int id)
        {
            var equipment = await _context.Equipment
                .Include(eq => eq.WorkPlace)
                .FirstOrDefaultAsync(eq => eq.EquipmentID == id);

            if (equipment == null)
            {
                return NotFound();
            }

            return new
            {
                equipment.EquipmentID,
                equipment.EquipmentName,
                equipment.EquipmentType,
                equipment.WorkPlaceID,
                WorkPlace = equipment.WorkPlace != null ? new
                {
                    equipment.WorkPlace.WorkPlaceID,
                    equipment.WorkPlace.Name,
                    equipment.WorkPlace.Location,
                    equipment.WorkPlace.Notes
                } : null
            };
        }

        // POST: api/Equipment
        [HttpPost]
        public async Task<ActionResult<Equipment>> CreateEquipment(Equipment equipment)
        {
            _context.Equipment.Add(equipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEquipment), new { id = equipment.EquipmentID }, equipment);
        }

        // PUT: api/Equipment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEquipment(int id, Equipment equipment)
        {
            if (id != equipment.EquipmentID)
            {
                return BadRequest();
            }

            _context.Entry(equipment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EquipmentExists(id))
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

        // DELETE: api/Equipment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEquipment(int id)
        {
            var equipment = await _context.Equipment.FindAsync(id);
            if (equipment == null)
            {
                return NotFound();
            }

            _context.Equipment.Remove(equipment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EquipmentExists(int id)
        {
            return _context.Equipment.Any(e => e.EquipmentID == id);
        }
    }
}