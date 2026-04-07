using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShiftWorkLogController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public ShiftWorkLogController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/ShiftWorkLog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetShiftWorkLogs()
        {
            return await _context.ShiftWorkLogs
                .Include(swl => swl.Master)
                .Select(swl => new
                {
                    swl.ShiftWorkLogID,
                    swl.WorkDate,
                    swl.ShiftNumber,
                    swl.MasterID,
                    Master = swl.Master != null ? new
                    {
                        swl.Master.PersonID,
                        swl.Master.FullName,
                        swl.Master.Role
                    } : null,
                    SetupPeopleCount = swl.SetupPeople != null ? swl.SetupPeople.Count : 0,
                    EquipmentCount = swl.Equipments != null ? swl.Equipments.Count : 0,
                    swl.Notes
                })
                .ToListAsync();
        }

        // GET: api/ShiftWorkLog/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetShiftWorkLog(int id)
        {
            var log = await _context.ShiftWorkLogs
                .Include(swl => swl.Master)
                .Include(swl => swl.SetupPeople)
                    .ThenInclude(sp => sp.Person)
                .Include(swl => swl.Equipments)
                    .ThenInclude(se => se.Equipment)
                .FirstOrDefaultAsync(swl => swl.ShiftWorkLogID == id);

            if (log == null)
            {
                return NotFound();
            }

            return new
            {
                log.ShiftWorkLogID,
                log.WorkDate,
                log.ShiftNumber,
                log.MasterID,
                Master = log.Master != null ? new
                {
                    log.Master.PersonID,
                    log.Master.FullName,
                    log.Master.Role
                } : null,
                SetupPeople = log.SetupPeople != null ? log.SetupPeople.Select(sp => new
                {
                    sp.PersonID,
                    Person = sp.Person != null ? new
                    {
                        sp.Person.PersonID,
                        sp.Person.FullName,
                        sp.Person.Role
                    } : null
                }) : null,
                Equipments = log.Equipments != null ? log.Equipments.Select(se => new
                {
                    se.EquipmentID,
                    Equipment = se.Equipment != null ? new
                    {
                        se.Equipment.EquipmentID,
                        se.Equipment.EquipmentName,
                        se.Equipment.EquipmentType
                    } : null
                }) : null,
                log.Notes
            };
        }

        // POST: api/ShiftWorkLog
        [HttpPost]
        public async Task<ActionResult<ShiftWorkLog>> CreateShiftWorkLog(ShiftWorkLog log)
        {
            _context.ShiftWorkLogs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShiftWorkLog), new { id = log.ShiftWorkLogID }, log);
        }

        // PUT: api/ShiftWorkLog/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShiftWorkLog(int id, ShiftWorkLog log)
        {
            if (id != log.ShiftWorkLogID)
            {
                return BadRequest();
            }

            _context.Entry(log).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShiftWorkLogExists(id))
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

        // DELETE: api/ShiftWorkLog/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShiftWorkLog(int id)
        {
            var log = await _context.ShiftWorkLogs.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            _context.ShiftWorkLogs.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ShiftWorkLogExists(int id)
        {
            return _context.ShiftWorkLogs.Any(swl => swl.ShiftWorkLogID == id);
        }
    }
}