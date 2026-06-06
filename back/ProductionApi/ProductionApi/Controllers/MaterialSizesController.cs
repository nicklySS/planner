using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialSizesController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public MaterialSizesController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/MaterialSizes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaterialSize>>> GetMaterialSizes()
        {
            return await _context.MaterialSizes
                .Include(ms => ms.MaterialMaterialSizes)
                    .ThenInclude(mms => mms.Material)
                .ToListAsync();
        }

        // GET: api/MaterialSizes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaterialSize>> GetMaterialSize(int id)
        {
            var size = await _context.MaterialSizes
                .Include(ms => ms.MaterialMaterialSizes)
                    .ThenInclude(mms => mms.Material)
                .FirstOrDefaultAsync(ms => ms.MaterialSizeID == id);

            if (size == null)
            {
                return NotFound();
            }

            return size;
        }

        // POST: api/MaterialSizes
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<MaterialSize>> CreateMaterialSize(MaterialSize size)
        {
            _context.MaterialSizes.Add(size);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaterialSize), new { id = size.MaterialSizeID }, size);
        }

        // PUT: api/MaterialSizes/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaterialSize(int id, MaterialSize size)
        {
            if (id != size.MaterialSizeID)
            {
                return BadRequest();
            }

            _context.Entry(size).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaterialSizeExists(id))
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

        // DELETE: api/MaterialSizes/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterialSize(int id)
        {
            var size = await _context.MaterialSizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }

            // Удаляем все зависимые записи
            
            // 1. Удаляем связи Material <-> MaterialSize
            var materialSizeLinks = await _context.MaterialMaterialSizes
                .Where(mms => mms.MaterialSizeID == id)
                .ToListAsync();
            _context.MaterialMaterialSizes.RemoveRange(materialSizeLinks);
            
            // 2. Удаляем остатки материала
            var materialStocks = await _context.MaterialStocks
                .Where(ms => ms.MaterialSizeID == id)
                .ToListAsync();
            _context.MaterialStocks.RemoveRange(materialStocks);
            
            // 3. Удаляем транзакции материала
            var materialTransactions = await _context.MaterialTransactions
                .Where(mt => mt.MaterialSizeID == id)
                .ToListAsync();
            _context.MaterialTransactions.RemoveRange(materialTransactions);
            
            // 4. Удаляем размер
            _context.MaterialSizes.Remove(size);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Размер и все зависимые записи успешно удалены" });
        }

        private bool MaterialSizeExists(int id)
        {
            return _context.MaterialSizes.Any(ms => ms.MaterialSizeID == id);
        }
    }
}
