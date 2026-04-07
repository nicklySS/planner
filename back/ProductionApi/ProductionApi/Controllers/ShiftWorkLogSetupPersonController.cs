using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftWorkLogSetupPersonController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public ShiftWorkLogSetupPersonController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/ShiftWorkLogSetupPerson
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShiftWorkLogSetupPerson>>> GetAll()
        {
            return await _context.ShiftWorkLogSetupPeople
                .Include(sp => sp.ShiftWorkLog)
                .Include(sp => sp.Person)
                .ToListAsync();
        }

        // GET: api/ShiftWorkLogSetupPerson/5/10
        [HttpGet("{shiftWorkLogId}/{personId}")]
        public async Task<ActionResult<ShiftWorkLogSetupPerson>> Get(int shiftWorkLogId, int personId)
        {
            var entry = await _context.ShiftWorkLogSetupPeople
                .Include(sp => sp.ShiftWorkLog)
                .Include(sp => sp.Person)
                .FirstOrDefaultAsync(sp => sp.ShiftWorkLogID == shiftWorkLogId && sp.PersonID == personId);

            if (entry == null)
            {
                return NotFound();
            }

            return entry;
        }

        // POST: api/ShiftWorkLogSetupPerson
        [HttpPost]
        public async Task<ActionResult<ShiftWorkLogSetupPerson>> Create(ShiftWorkLogSetupPerson sp)
        {
            _context.ShiftWorkLogSetupPeople.Add(sp);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { shiftWorkLogId = sp.ShiftWorkLogID, personId = sp.PersonID }, sp);
        }

        // DELETE: api/ShiftWorkLogSetupPerson/5/10
        [HttpDelete("{shiftWorkLogId}/{personId}")]
        public async Task<IActionResult> Delete(int shiftWorkLogId, int personId)
        {
            var entry = await _context.ShiftWorkLogSetupPeople
                .FirstOrDefaultAsync(sp => sp.ShiftWorkLogID == shiftWorkLogId && sp.PersonID == personId);

            if (entry == null)
            {
                return NotFound();
            }

            _context.ShiftWorkLogSetupPeople.Remove(entry);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool Exists(int shiftWorkLogId, int personId)
        {
            return _context.ShiftWorkLogSetupPeople.Any(sp => sp.ShiftWorkLogID == shiftWorkLogId && sp.PersonID == personId);
        }
    }
}
