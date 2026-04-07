using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetailController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public DetailController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/Detail
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDetails()
        {
            return await _context.Details
                .Select(d => new
                {
                    d.DetailID,
                    d.DetailName,
                    OperationsCount = d.Operations != null ? d.Operations.Count : 0,
                    FromReconfigurationsCount = d.FromReconfigurations != null ? d.FromReconfigurations.Count : 0,
                    ToReconfigurationsCount = d.ToReconfigurations != null ? d.ToReconfigurations.Count : 0
                })
                .ToListAsync();

            //return await _context.Details
            //    .Select(d => new
            //    {
            //        d.DetailID,
            //        d.DetailName,
            //        OperationsCount = d.Operations != null ? d.Operations.Count : 0,
            //        FromReconfigurationsCount = d.FromReconfigurations != null ? d.FromReconfigurations.Count : 0,
            //        ToReconfigurationsCount = d.ToReconfigurations != null ? d.ToReconfigurations.Count : 0
            //    })
            //    .ToListAsync();


        }

        // GET: api/Detail/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDetail(int id)
        {
            var detail = await _context.Details
                .FirstOrDefaultAsync(d => d.DetailID == id);

            if (detail == null)
            {
                return NotFound();
            }

            return new
            {
                detail.DetailID,
                detail.DetailName
            };
        }

        // POST: api/Detail
        [HttpPost]
        public async Task<ActionResult<Detail>> CreateDetail(Detail detail)
        {
            _context.Details.Add(detail);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetail), new { id = detail.DetailID }, detail);
        }

        // PUT: api/Detail/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDetail(int id, Detail detail)
        {
            if (id != detail.DetailID)
            {
                return BadRequest();
            }

            _context.Entry(detail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DetailExists(id))
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

        // DELETE: api/Detail/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDetail(int id)
        {
            var detail = await _context.Details.FindAsync(id);
            if (detail == null)
            {
                return NotFound();
            }

            _context.Details.Remove(detail);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DetailExists(int id)
        {
            return _context.Details.Any(d => d.DetailID == id);
        }
    }
}