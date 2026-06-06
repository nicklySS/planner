using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftWorkLogEquipmentController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public ShiftWorkLogEquipmentController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/ShiftWorkLogEquipment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftWorkLogEquipment>>> GetAll()
        {
            return await _context.ShiftWorkLogEquipments
                .Include(se => se.ShiftWorkLog)
                .Include(se => se.Equipment)
                .ToListAsync();
        }

        // GET: api/ShiftWorkLogEquipment/5/10
        [HttpGet("{shiftWorkLogId}/{equipmentId}")]
        public async Task<ActionResult<ShiftWorkLogEquipment>> Get(int shiftWorkLogId, int equipmentId)
        {
            var entry = await _context.ShiftWorkLogEquipments
                .Include(se => se.ShiftWorkLog)
                .Include(se => se.Equipment)
                .FirstOrDefaultAsync(se => se.ShiftWorkLogID == shiftWorkLogId && se.EquipmentID == equipmentId);

            if (entry == null)
            {
                return NotFound();
            }

            return entry;
        }

        // POST: api/ShiftWorkLogEquipment
        [Authorize(Policy = AuthorizationPolicies.CanWriteShifts)]
        [HttpPost]
        public async Task<ActionResult<ShiftWorkLogEquipment>> Create(ShiftWorkLogEquipment se)
        {
            _context.ShiftWorkLogEquipments.Add(se);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { shiftWorkLogId = se.ShiftWorkLogID, equipmentId = se.EquipmentID }, se);
        }

        // DELETE: api/ShiftWorkLogEquipment/5/10
        [Authorize(Policy = AuthorizationPolicies.CanWriteShifts)]
        [HttpDelete("{shiftWorkLogId}/{equipmentId}")]
        public async Task<IActionResult> Delete(int shiftWorkLogId, int equipmentId)
        {
            var entry = await _context.ShiftWorkLogEquipments
                .FirstOrDefaultAsync(se => se.ShiftWorkLogID == shiftWorkLogId && se.EquipmentID == equipmentId);

            if (entry == null)
            {
                return NotFound();
            }

            _context.ShiftWorkLogEquipments.Remove(entry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Exists(int shiftWorkLogId, int equipmentId)
        {
            return _context.ShiftWorkLogEquipments.Any(se => se.ShiftWorkLogID == shiftWorkLogId && se.EquipmentID == equipmentId);
        }
    }
}
