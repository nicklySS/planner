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
                .Include(o => o.MaterialSize)
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
                .Include(o => o.MaterialSize)
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
                .Include(o => o.MaterialSize)
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

        // GET: api/Operations/material-sizes?detailId=5
        // Получить размерности материала детали, сгруппированные по типу (Unit)
        [HttpGet("material-sizes")]
        public async Task<ActionResult<object>> GetDetailMaterialSizes([FromQuery] int detailId)
        {
            var detail = await _context.Details
                .Include(d => d.Material!)
                    .ThenInclude(m => m.MaterialMaterialSizes!)
                        .ThenInclude(mms => mms.MaterialSize)
                .FirstOrDefaultAsync(d => d.DetailID == detailId);

            if (detail == null || detail.Material == null)
            {
                return NotFound();
            }

            // Получить все размерности материала и сгруппировать по типу (Unit)
            var materialSizesByUnit = new Dictionary<string, List<object>>();
            
            if (detail.Material.MaterialMaterialSizes != null)
            {
                materialSizesByUnit = detail.Material.MaterialMaterialSizes
                    .Select(mms => mms.MaterialSize)
                    .Where(ms => ms != null)
                    .GroupBy(ms => ms!.Unit)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ms => new
                        {
                            ms!.MaterialSizeID,
                            ms.SizeValue,
                            ms.Unit
                        }).Cast<object>().ToList()
                    );
            }

            return Ok(new
            {
                detail.DetailID,
                detail.DetailName,
                detail.Material.MaterialID,
                detail.Material.MaterialName,
                MaterialSizesByUnit = materialSizesByUnit
            });
        }

        // GET: api/Operations/material-sizes/{operationId}
        // Получить размерности материала операции, сгруппированные по типу (Unit)
        [HttpGet("material-sizes/{operationId}")]
        public async Task<ActionResult<object>> GetOperationMaterialSizes(int operationId)
        {
            var operation = await _context.Operations
                .Include(o => o.Detail)
                    .ThenInclude(d => d.Material!)
                        .ThenInclude(m => m.MaterialMaterialSizes!)
                            .ThenInclude(mms => mms.MaterialSize)
                .FirstOrDefaultAsync(o => o.OperationID == operationId);

            if (operation == null || operation.Detail == null || operation.Detail.Material == null)
            {
                return NotFound();
            }

            // Получить все размерности материала и сгруппировать по типу (Unit)
            var materialSizesByUnit = new Dictionary<string, List<object>>();
            
            if (operation.Detail.Material.MaterialMaterialSizes != null)
            {
                materialSizesByUnit = operation.Detail.Material.MaterialMaterialSizes
                    .Select(mms => mms.MaterialSize)
                    .Where(ms => ms != null)
                    .GroupBy(ms => ms!.Unit)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(ms => new
                        {
                            ms!.MaterialSizeID,
                            ms.SizeValue,
                            ms.Unit
                        }).Cast<object>().ToList()
                    );
            }

            return Ok(new
            {
                operation.OperationID,
                operation.Detail.DetailID,
                operation.Detail.DetailName,
                operation.Detail.Material.MaterialID,
                operation.Detail.Material.MaterialName,
                MaterialSizesByUnit = materialSizesByUnit
            });
        }
    }
}
