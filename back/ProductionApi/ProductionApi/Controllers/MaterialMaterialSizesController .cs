using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialMaterialSizesController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public MaterialMaterialSizesController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/MaterialMaterialSizes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaterialMaterialSize>>> GetMaterialMaterialSizes()
        {
            return await _context.MaterialMaterialSizes
                .Include(mms => mms.Material)
                .Include(mms => mms.MaterialSize)
                .ToListAsync();
        }

        // GET: api/MaterialMaterialSizes/5/10
        [HttpGet("{materialId}/{sizeId}")]
        public async Task<ActionResult<MaterialMaterialSize>> GetMaterialMaterialSize(int materialId, int sizeId)
        {
            var mms = await _context.MaterialMaterialSizes
                .Include(m => m.Material)
                .Include(ms => ms.MaterialSize)
                .FirstOrDefaultAsync(x => x.MaterialID == materialId && x.MaterialSizeID == sizeId);

            if (mms == null)
            {
                return NotFound();
            }

            return mms;
        }

        // POST: api/MaterialMaterialSizes
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<MaterialMaterialSize>> CreateMaterialMaterialSize(MaterialMaterialSize mms)
        {
            _context.MaterialMaterialSizes.Add(mms);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaterialMaterialSize),
                new { materialId = mms.MaterialID, sizeId = mms.MaterialSizeID },
                mms);
        }

        // DELETE: api/MaterialMaterialSizes/5/10
        [AdminWrite]
        [HttpDelete("{materialId}/{sizeId}")]
        public async Task<IActionResult> DeleteMaterialMaterialSize(int materialId, int sizeId)
        {
            var mms = await _context.MaterialMaterialSizes
                .FirstOrDefaultAsync(x => x.MaterialID == materialId && x.MaterialSizeID == sizeId);

            if (mms == null)
            {
                return NotFound();
            }

            _context.MaterialMaterialSizes.Remove(mms);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MaterialMaterialSizeExists(int materialId, int sizeId)
        {
            return _context.MaterialMaterialSizes.Any(x => x.MaterialID == materialId && x.MaterialSizeID == sizeId);
        }
    }
}
