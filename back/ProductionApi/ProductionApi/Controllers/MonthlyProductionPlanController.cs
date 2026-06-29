using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Auth;
using ProductionApi.Data;
using ProductionApi.Models;
using ProductionApi.Services;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonthlyProductionPlanController : ControllerBase
    {
        private readonly ProductionDbContext _context;
        private readonly PlannerExcelService _excelService;

        public MonthlyProductionPlanController(ProductionDbContext context, PlannerExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        [HttpGet("{year}/{month}")]
        public async Task<ActionResult> GetPlan(int year, int month)
        {
            var plan = await _context.MonthlyProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            if (plan == null)
                return Ok(new { year, month, items = Array.Empty<object>() });

            return Ok(new
            {
                plan.PlanID,
                plan.Year,
                plan.Month,
                plan.Notes,
                plan.CreatedAt,
                items = plan.Items!.Select(i => new
                {
                    i.PlanItemID,
                    i.DetailID,
                    DetailName = i.Detail?.DetailName,
                    DetailCode = i.Detail?.DetailCode,
                    DetailFullName = i.Detail != null
                        ? (string.IsNullOrWhiteSpace(i.Detail.DetailCode)
                            ? i.Detail.DetailName
                            : $"{i.Detail.DetailName} ({i.Detail.DetailCode})")
                        : null,
                    i.Quantity,
                    ShipmentDate = i.ShipmentDate.ToString("yyyy-MM-dd"),
                    i.Notes
                })
            });
        }

        [AdminWrite]
        [HttpPut("{year}/{month}")]
        public async Task<ActionResult> SavePlan(int year, int month, [FromBody] SaveMonthlyPlanDto dto)
        {
            var plan = await _context.MonthlyProductionPlans
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            if (plan == null)
            {
                plan = new MonthlyProductionPlan
                {
                    Year = year,
                    Month = month,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MonthlyProductionPlans.Add(plan);
                await _context.SaveChangesAsync();
            }

            plan.Notes = dto.Notes;

            if (plan.Items != null && plan.Items.Any())
                _context.MonthlyProductionPlanItems.RemoveRange(plan.Items);

            if (dto.Items != null)
            {
                foreach (var item in dto.Items)
                {
                    _context.MonthlyProductionPlanItems.Add(new MonthlyProductionPlanItem
                    {
                        PlanID = plan.PlanID,
                        DetailID = item.DetailId,
                        Quantity = item.Quantity,
                        ShipmentDate = DateTime.Parse(item.ShipmentDate).Date,
                        Notes = item.Notes
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GetPlan(year, month);
        }

        [AdminWrite]
        [HttpPost("import/{year}/{month}")]
        public async Task<ActionResult> ImportPlanFromExcel(int year, int month, IFormFile file)
        {
            try
            {
                var result = await _excelService.ImportShipmentPlanAsync(file, year, month, _context);
                return Ok(new { message = "План отгрузок импортирован", importedRows = result.ImportedRows });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AdminWrite]
        [HttpGet("export/{year}/{month}")]
        public async Task<IActionResult> ExportPlanToExcel(int year, int month)
        {
            var stream = await _excelService.ExportShipmentPlanAsync(year, month, _context);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"plan-otgruzok-{year}-{month:D2}.xlsx");
        }

        [AdminWrite]
        [HttpDelete("items/{itemId}")]
        public async Task<IActionResult> DeleteItem(int itemId)
        {
            var item = await _context.MonthlyProductionPlanItems.FindAsync(itemId);
            if (item == null) return NotFound();

            _context.MonthlyProductionPlanItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class SaveMonthlyPlanDto
    {
        public string? Notes { get; set; }
        public List<MonthlyPlanItemDto>? Items { get; set; }
    }

    public class MonthlyPlanItemDto
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
        public string ShipmentDate { get; set; } = null!;
        public string? Notes { get; set; }
    }
}
