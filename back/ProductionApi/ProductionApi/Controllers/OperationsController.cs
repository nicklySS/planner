using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperationsController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public OperationsController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/Operations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Operation>>> GetOperations([FromQuery] int? detailID = null)
        {
            var query = _context.Operations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .AsQueryable();

            if (detailID.HasValue)
            {
                query = query.Where(o => o.DetailID == detailID.Value);
            }

            return await query.ToListAsync();
        }

        // GET: api/Operations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Operation>> GetOperation(int id)
        {
            var operation = await _context.Operations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .FirstOrDefaultAsync(o => o.OperationID == id);

            if (operation == null)
            {
                return NotFound();
            }

            return operation;
        }

        // POST: api/Operations
        [HttpPost]
        public async Task<ActionResult<Operation>> CreateOperation(Operation operation)
        {
            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            // Reload with includes
            var createdOperation = await _context.Operations
                .Include(o => o.Equipment)
                .Include(o => o.Detail)
                .FirstOrDefaultAsync(o => o.OperationID == operation.OperationID);

            return CreatedAtAction(nameof(GetOperation), new { id = operation.OperationID }, createdOperation);
        }

        // PUT: api/Operations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOperation(int id, Operation operation)
        {
            if (id != operation.OperationID)
            {
                return BadRequest();
            }

            _context.Entry(operation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OperationExists(id))
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

        // DELETE: api/Operations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOperation(int id)
        {
            var operation = await _context.Operations.FindAsync(id);
            if (operation == null)
            {
                return NotFound();
            }

            _context.Operations.Remove(operation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OperationExists(int id)
        {
            return _context.Operations.Any(o => o.OperationID == id);
        }
    }
}
