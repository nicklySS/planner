using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Services;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ProductionDbContext _context;
        private readonly PasswordService _passwordService;

        public AuthController(
            ProductionDbContext context,
            PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeNumber) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Укажите табельный номер и пароль" });

            var employeeNumber = request.EmployeeNumber.Trim();
            var people = await _context.People
                .Include(p => p.PersonRoles!)
                    .ThenInclude(pr => pr.Role)
                .Where(p => p.EmployeeNumber != null)
                .ToListAsync();

            var person = people.FirstOrDefault(p =>
                string.Equals(p.EmployeeNumber!.Trim(), employeeNumber, StringComparison.OrdinalIgnoreCase));

            if (person == null || !person.IsActive)
                return Unauthorized(new { message = "Неверный табельный номер или пароль" });

            var isFirstLogin = string.IsNullOrWhiteSpace(person.PasswordHash);
            if (isFirstLogin)
            {
                person.PasswordHash = _passwordService.HashPassword(person, request.Password);
                person.EmployeeNumber = person.EmployeeNumber?.Trim();
                await _context.SaveChangesAsync();
            }
            else if (!_passwordService.VerifyPassword(person, request.Password, person.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный табельный номер или пароль" });
            }

            var roles = person.PersonRoles?
                .Where(pr => pr.Role != null)
                .Select(pr => pr.Role!.RoleName)
                .Distinct()
                .ToList() ?? new List<string>();

            if (roles.Count == 0)
                return Unauthorized(new { message = "У пользователя не назначена роль" });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, person.PersonID.ToString()),
                new(ClaimTypes.Name, person.FullName ?? person.EmployeeNumber ?? person.PersonID.ToString())
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return Ok(new
            {
                person.PersonID,
                person.FullName,
                person.EmployeeNumber,
                roles,
                isFirstLogin
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { message = "Укажите новый пароль" });

            var personIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(personIdClaim, out var personId))
                return Unauthorized();

            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonID == personId);
            if (person == null || !person.IsActive)
                return Unauthorized();

            if (!string.IsNullOrWhiteSpace(person.PasswordHash))
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                    return BadRequest(new { message = "Укажите текущий пароль" });

                if (!_passwordService.VerifyPassword(person, request.CurrentPassword, person.PasswordHash))
                    return BadRequest(new { message = "Неверный текущий пароль" });
            }

            person.PasswordHash = _passwordService.HashPassword(person, request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пароль изменён" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Выход выполнен" });
        }

        [HttpGet("me")]
        public async Task<ActionResult> Me()
        {
            var personIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(personIdClaim, out var personId))
                return Unauthorized();

            var person = await _context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PersonID == personId);

            if (person == null || !person.IsActive)
                return Unauthorized();

            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                person.PersonID,
                person.FullName,
                person.EmployeeNumber,
                roles
            });
        }
    }

    public class LoginRequest
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
