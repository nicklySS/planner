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
    public class DetailOperationsController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public DetailOperationsController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/DetailOperations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DetailOperation>>> GetDetailOperations([FromQuery] int? detailID = null)
        {
            var query = _context.DetailOperations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .AsQueryable();

            if (detailID.HasValue)
            {
                query = query.Where(o => o.DetailID == detailID.Value);
            }

            // Сортировка по №п/п (SequenceNumber)
            query = query.OrderBy(o => o.SequenceNumber).ThenBy(o => o.DetailOperationID);

            return await query.ToListAsync();
        }

        // GET: api/DetailOperations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DetailOperation>> GetDetailOperation(int id)
        {
            var detailOperation = await _context.DetailOperations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .FirstOrDefaultAsync(o => o.DetailOperationID == id);

            if (detailOperation == null)
            {
                return NotFound();
            }

            return detailOperation;
        }

        // GET: api/DetailOperations/by-detail/{detailID}
        [HttpGet("by-detail/{detailID}")]
        public async Task<ActionResult<IEnumerable<DetailOperation>>> GetDetailOperationsByDetail(int detailID)
        {
            var detailOperations = await _context.DetailOperations
                .Include(o => o.Equipment)
                .Where(o => o.DetailID == detailID)
                .OrderBy(o => o.SequenceNumber)
                .ThenBy(o => o.DetailOperationID)
                .ToListAsync();

            return detailOperations;
        }

        // POST: api/DetailOperations
        [Authorize(Policy = AuthorizationPolicies.CanWriteDetails)]
        [HttpPost]
        public async Task<ActionResult<DetailOperation>> CreateDetailOperation(DetailOperation detailOperation)
        {
            _context.DetailOperations.Add(detailOperation);
            await _context.SaveChangesAsync();

            // Reload with includes
            var createdDetailOperation = await _context.DetailOperations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .FirstOrDefaultAsync(o => o.DetailOperationID == detailOperation.DetailOperationID);

            return CreatedAtAction(nameof(GetDetailOperation), new { id = detailOperation.DetailOperationID }, createdDetailOperation);
        }

        // PUT: api/DetailOperations/5
        [Authorize(Policy = AuthorizationPolicies.CanWriteDetails)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDetailOperation(int id, DetailOperation detailOperation)
        {
            if (id != detailOperation.DetailOperationID)
            {
                return BadRequest();
            }

            _context.Entry(detailOperation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DetailOperationExists(id))
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

        // DELETE: api/DetailOperations/5
        [Authorize(Policy = AuthorizationPolicies.CanWriteDetails)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDetailOperation(int id)
        {
            var detailOperation = await _context.DetailOperations.FindAsync(id);
            if (detailOperation == null)
            {
                return NotFound();
            }

            _context.DetailOperations.Remove(detailOperation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DetailOperationExists(int id)
        {
            return _context.DetailOperations.Any(o => o.DetailOperationID == id);
        }
    }
}
