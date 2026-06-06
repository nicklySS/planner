using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public MaterialsController(ProductionDbContext context)
        {
            _context = context;
        }

        // DTO for creating/updating materials with sizes
        public class MaterialDto
        {
            public int MaterialID { get; set; }
            public string MaterialName { get; set; } = null!;
            public List<MaterialSizeDto>? MaterialMaterialSizes { get; set; }
        }

        public class MaterialSizeDto
        {
            public int MaterialID { get; set; }
            public int MaterialSizeID { get; set; }
        }

        // GET: api/Materials
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials()
        {
            return await _context.Materials
                .Include(m => m.MaterialMaterialSizes)
                    .ThenInclude(mms => mms.MaterialSize)
                .ToListAsync();
        }

        // GET: api/Materials/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterial(int id)
        {
            var material = await _context.Materials
                .Include(m => m.MaterialMaterialSizes)
                    .ThenInclude(mms => mms.MaterialSize)
                .FirstOrDefaultAsync(m => m.MaterialID == id);

            if (material == null)
            {
                return NotFound();
            }

            return material;
        }

        // POST: api/Materials
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<Material>> CreateMaterial(MaterialDto materialDto)
        {
            var material = new Material
            {
                MaterialName = materialDto.MaterialName,
                MaterialMaterialSizes = new List<MaterialMaterialSize>()
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            // Add material sizes if provided
            if (materialDto.MaterialMaterialSizes != null && materialDto.MaterialMaterialSizes.Count > 0)
            {
                foreach (var sizeDto in materialDto.MaterialMaterialSizes)
                {
                    var materialSize = new MaterialMaterialSize
                    {
                        MaterialID = material.MaterialID,
                        MaterialSizeID = sizeDto.MaterialSizeID
                    };
                    _context.MaterialMaterialSizes.Add(materialSize);
                }
                await _context.SaveChangesAsync();
            }

            // Reload material with sizes
            var createdMaterial = await _context.Materials
                .Include(m => m.MaterialMaterialSizes)
                    .ThenInclude(mms => mms.MaterialSize)
                .FirstOrDefaultAsync(m => m.MaterialID == material.MaterialID);

            return CreatedAtAction(nameof(GetMaterial), new { id = material.MaterialID }, createdMaterial);
        }

        // PUT: api/Materials/5
        [AdminWrite]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaterial(int id, MaterialDto materialDto)
        {
            if (id != materialDto.MaterialID)
            {
                return BadRequest();
            }

            var material = await _context.Materials
                .Include(m => m.MaterialMaterialSizes)
                .FirstOrDefaultAsync(m => m.MaterialID == id);

            if (material == null)
            {
                return NotFound();
            }

            material.MaterialName = materialDto.MaterialName;

            // Update material sizes
            // Remove old sizes
            _context.MaterialMaterialSizes.RemoveRange(material.MaterialMaterialSizes!);

            // Add new sizes
            if (materialDto.MaterialMaterialSizes != null && materialDto.MaterialMaterialSizes.Count > 0)
            {
                foreach (var sizeDto in materialDto.MaterialMaterialSizes)
                {
                    var materialSize = new MaterialMaterialSize
                    {
                        MaterialID = id,
                        MaterialSizeID = sizeDto.MaterialSizeID
                    };
                    _context.MaterialMaterialSizes.Add(materialSize);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaterialExists(id))
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

        // DELETE: api/Materials/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool MaterialExists(int id)
        {
            return _context.Materials.Any(m => m.MaterialID == id);
        }
    }
}
