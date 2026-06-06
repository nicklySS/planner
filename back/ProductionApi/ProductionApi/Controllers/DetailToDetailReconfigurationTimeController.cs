using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetailToDetailReconfigurationTimeController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public DetailToDetailReconfigurationTimeController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/DetailToDetailReconfigurationTime
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetailToDetailReconfigurationTime>>> GetReconfigurations()
        {
            return await _context.DetailToDetailReconfigurationTimes
                .Include(r => r.Equipment)
                .Include(r => r.FromDetail)
                .Include(r => r.ToDetail)
                .ToListAsync();
        }

        // GET: api/DetailToDetailReconfigurationTime/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DetailToDetailReconfigurationTime>> GetReconfiguration(int id)
        {
            var reconfiguration = await _context.DetailToDetailReconfigurationTimes
                .Include(r => r.Equipment)
                .Include(r => r.FromDetail)
                .Include(r => r.ToDetail)
                .FirstOrDefaultAsync(r => r.ReconfigurationID == id);

            if (reconfiguration == null)
            {
                return NotFound();
            }

            return reconfiguration;
        }

        // POST: api/DetailToDetailReconfigurationTime
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<DetailToDetailReconfigurationTime>> CreateReconfiguration(DetailToDetailReconfigurationTime reconfiguration)
        {
            _context.DetailToDetailReconfigurationTimes.Add(reconfiguration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReconfiguration), new { id = reconfiguration.ReconfigurationID }, reconfiguration);
        }

        // PUT: api/DetailToDetailReconfigurationTime/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReconfiguration(int id, DetailToDetailReconfigurationTime reconfiguration)
        {
            if (id != reconfiguration.ReconfigurationID)
            {
                return BadRequest();
            }

            _context.Entry(reconfiguration).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReconfigurationExists(id))
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

        // DELETE: api/DetailToDetailReconfigurationTime/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReconfiguration(int id)
        {
            var reconfiguration = await _context.DetailToDetailReconfigurationTimes.FindAsync(id);
            if (reconfiguration == null)
            {
                return NotFound();
            }

            _context.DetailToDetailReconfigurationTimes.Remove(reconfiguration);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReconfigurationExists(int id)
        {
            return _context.DetailToDetailReconfigurationTimes.Any(r => r.ReconfigurationID == id);
        }
    }
}
