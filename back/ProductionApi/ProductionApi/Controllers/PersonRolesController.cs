using Microsoft.AspNetCore.Mvc;
using ProductionApi.Auth;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonRolesController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public PersonRolesController(ProductionDbContext context)
        {
            _context = context;
        }

        // GET: api/PersonRoles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PersonRole>>> GetPersonRoles()
        {
            return await _context.PersonRoles
                .Include(pr => pr.Person)
                .Include(pr => pr.Role)
                .ToListAsync();
        }

        // GET: api/PersonRoles/ByPerson/5
        [HttpGet("ByPerson/{personId}")]
        public async Task<ActionResult<IEnumerable<PersonRole>>> GetPersonRolesByPerson(int personId)
        {
            return await _context.PersonRoles
                .Where(pr => pr.PersonID == personId)
                .Include(pr => pr.Role)
                .ToListAsync();
        }

        // POST: api/PersonRoles
        [AdminWrite]
        [HttpPost]
        public async Task<ActionResult<PersonRole>> CreatePersonRole(PersonRole personRole)
        {
            // Проверяем, что связь ещё не существует
            var exists = await _context.PersonRoles
                .AnyAsync(pr => pr.PersonID == personRole.PersonID && pr.RoleID == personRole.RoleID);

            if (exists)
            {
                return BadRequest("This person already has this role");
            }

            _context.PersonRoles.Add(personRole);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPersonRoles), new { id = personRole.PersonRoleID }, personRole);
        }

        // DELETE: api/PersonRoles/5
        [AdminWrite]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersonRole(int id)
        {
            var personRole = await _context.PersonRoles.FindAsync(id);
            if (personRole == null)
            {
                return NotFound();
            }

            _context.PersonRoles.Remove(personRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/PersonRoles/RemoveByPersonAndRole/5/3
        [AdminWrite]
        [HttpDelete("RemoveByPersonAndRole/{personId}/{roleId}")]
        public async Task<IActionResult> DeletePersonRoleByPersonAndRole(int personId, int roleId)
        {
            var personRole = await _context.PersonRoles
                .FirstOrDefaultAsync(pr => pr.PersonID == personId && pr.RoleID == roleId);

            if (personRole == null)
            {
                return NotFound();
            }

            _context.PersonRoles.Remove(personRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
