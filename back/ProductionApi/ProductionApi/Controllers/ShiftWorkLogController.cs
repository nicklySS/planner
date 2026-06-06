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
                    .ThenInclude(m => m.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                .Include(swl => swl.Worker)
                    .ThenInclude(w => w.WorkPlace)
                .Include(swl => swl.Detail)
                .Include(swl => swl.Material)
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
                        Roles = swl.Master.PersonRoles.Select(pr => new
                        {
                            pr.Role.RoleID,
                            pr.Role.RoleName
                        }).ToList()
                    } : null,
                    swl.WorkerID,
                    Worker = swl.Worker != null ? new
                    {
                        swl.Worker.PersonID,
                        swl.Worker.FullName,
                        WorkPlace = swl.Worker.WorkPlace != null ? new
                        {
                            swl.Worker.WorkPlace.WorkPlaceID,
                            swl.Worker.WorkPlace.Name
                        } : null
                    } : null,
                    swl.DetailID,
                    Detail = swl.Detail != null ? new
                    {
                        swl.Detail.DetailID,
                        swl.Detail.DetailName
                    } : null,
                    swl.Quantity,
                    swl.MaterialID,
                    Material = swl.Material != null ? new
                    {
                        swl.Material.MaterialID,
                        swl.Material.MaterialName
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
                    .ThenInclude(m => m.PersonRoles)
                    .ThenInclude(pr => pr.Role)
                .Include(swl => swl.Worker)
                    .ThenInclude(w => w.WorkPlace)
                .Include(swl => swl.Detail)
                .Include(swl => swl.Material)
                .Include(swl => swl.SetupPeople)
                    .ThenInclude(sp => sp.Person)
                    .ThenInclude(p => p.PersonRoles)
                    .ThenInclude(pr => pr.Role)
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
                    Roles = log.Master.PersonRoles.Select(pr => new
                    {
                        pr.Role.RoleID,
                        pr.Role.RoleName
                    }).ToList()
                } : null,
                log.WorkerID,
                Worker = log.Worker != null ? new
                {
                    log.Worker.PersonID,
                    log.Worker.FullName,
                    WorkPlace = log.Worker.WorkPlace != null ? new
                    {
                        log.Worker.WorkPlace.WorkPlaceID,
                        log.Worker.WorkPlace.Name
                    } : null
                } : null,
                log.DetailID,
                Detail = log.Detail != null ? new
                {
                    log.Detail.DetailID,
                    log.Detail.DetailName
                } : null,
                log.Quantity,
                log.MaterialID,
                Material = log.Material != null ? new
                {
                    log.Material.MaterialID,
                    log.Material.MaterialName
                } : null,
                SetupPeople = log.SetupPeople != null ? log.SetupPeople.Select(sp => new
                {
                    sp.PersonID,
                    Person = sp.Person != null ? new
                    {
                        sp.Person.PersonID,
                        sp.Person.FullName,
                        Roles = sp.Person.PersonRoles.Select(pr => new
                        {
                            pr.Role.RoleID,
                            pr.Role.RoleName
                        }).ToList()
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
        [Authorize(Policy = AuthorizationPolicies.CanWriteShifts)]
        [HttpPost]
        public async Task<ActionResult<ShiftWorkLog>> CreateShiftWorkLog(ShiftWorkLog log)
        {
            _context.ShiftWorkLogs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShiftWorkLog), new { id = log.ShiftWorkLogID }, log);
        }

        // PUT: api/ShiftWorkLog/5
        [Authorize(Policy = AuthorizationPolicies.CanWriteShifts)]
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
        [Authorize(Policy = AuthorizationPolicies.CanWriteShifts)]
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