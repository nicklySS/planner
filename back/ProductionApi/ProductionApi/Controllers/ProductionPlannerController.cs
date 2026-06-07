using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Auth;
using ProductionApi.Data;
using ProductionApi.Services;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionPlannerController : ControllerBase
    {
        private readonly ProductionDbContext _context;
        private readonly ProductionPlanningService _planningService;
        private readonly ProductionPlanExecutionService _executionService;

        public ProductionPlannerController(
            ProductionDbContext context,
            ProductionPlanningService planningService,
            ProductionPlanExecutionService executionService)
        {
            _context = context;
            _planningService = planningService;
            _executionService = executionService;
        }

        [HttpGet("generated/{year}/{month}")]
        public async Task<ActionResult> GetGeneratedPlan(int year, int month)
        {
            var plan = await _context.GeneratedProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                        .ThenInclude(d => d!.Material)
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Equipment)
                .Where(p => p.Year == year && p.Month == month)
                .OrderByDescending(p => p.GeneratedAt)
                .FirstOrDefaultAsync();

            if (plan == null)
                return Ok(new { year, month, status = "None", items = Array.Empty<object>() });

            return Ok(new
            {
                plan.GeneratedPlanID,
                plan.Year,
                plan.Month,
                plan.GeneratedAt,
                plan.ConfirmedAt,
                plan.Status,
                plan.Notes,
                items = plan.Items!
                    .OrderBy(i => i.WorkDate)
                    .ThenBy(i => i.ShiftCode)
                    .ThenBy(i => i.EquipmentID)
                    .Select(i => new
                    {
                        i.ItemID,
                        WorkDate = i.WorkDate.ToString("yyyy-MM-dd"),
                        i.ShiftCode,
                        i.EquipmentID,
                        EquipmentName = i.Equipment?.EquipmentName,
                        i.DetailID,
                        DetailName = i.Detail?.DetailName,
                        i.PlannedQuantity,
                        i.IsOverdue,
                        i.Notes
                    })
            });
        }

        [AdminWrite]
        [HttpPost("generate/{year}/{month}")]
        public async Task<ActionResult> GeneratePlan(int year, int month)
        {
            try
            {
                await _planningService.GeneratePlanAsync(year, month);
                return await GetGeneratedPlan(year, month);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AdminWrite]
        [HttpDelete("generated/{year}/{month}")]
        public async Task<ActionResult> ClearGeneratedPlan(int year, int month)
        {
            var plans = await _context.GeneratedProductionPlans
                .Where(p => p.Year == year && p.Month == month)
                .ToListAsync();

            if (!plans.Any())
                return Ok(new { message = "План уже пуст" });

            if (plans.Any(p => p.Status == "Confirmed"))
                return BadRequest(new { message = "Нельзя очистить подтверждённый план" });

            _context.GeneratedProductionPlans.RemoveRange(plans);
            await _context.SaveChangesAsync();
            return Ok(new { message = "План по сменам очищен" });
        }

        [AdminWrite]
        [HttpPost("confirm/{year}/{month}")]
        public async Task<ActionResult> ConfirmPlan(int year, int month)
        {
            try
            {
                var plan = await _executionService.ConfirmPlanAsync(year, month);
                return Ok(new
                {
                    message = "План подтверждён. Детали оприходованы, материалы списаны.",
                    plan.GeneratedPlanID,
                    plan.Status,
                    plan.ConfirmedAt
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [AdminWrite]
        [HttpPost("cancel/{year}/{month}")]
        public async Task<ActionResult> CancelPlan(int year, int month)
        {
            try
            {
                var plan = await _executionService.CancelPlanAsync(year, month);
                return Ok(new
                {
                    message = "Подтверждение отменено. Материалы возвращены, детали списаны со склада.",
                    plan.GeneratedPlanID,
                    plan.Status
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("materials/{year}/{month}")]
        public async Task<ActionResult> GetMaterialAnalysis(int year, int month)
        {
            var report = await _executionService.BuildMaterialReportAsync(year, month);

            return Ok(new
            {
                totalRequiredForShipmentKg = report.TotalRequiredForShipmentKg,
                totalRequiredForPlanKg = report.TotalRequiredForPlanKg,
                materials = report.Materials.Select(a => new
                {
                    a.MaterialID,
                    a.MaterialName,
                    requiredForShipmentKg = a.RequiredForShipmentKg,
                    requiredForPlanKg = a.RequiredForPlanKg,
                    a.AvailableKg,
                    shortageForShipmentKg = a.ShortageForShipmentKg,
                    shortageForPlanKg = a.ShortageForPlanKg,
                    usedForDetails = a.Details
                }),
                byDetail = report.ByDetail.Select(d => new
                {
                    d.DetailID,
                    d.DetailName,
                    d.DemandQuantity,
                    d.OnStock,
                    d.NetNeededForShipment,
                    d.PlannedQuantity,
                    d.MaterialID,
                    d.MaterialName,
                    d.ConsumptionRate,
                    requiredForShipmentKg = d.RequiredForShipmentKg,
                    requiredForPlanKg = d.RequiredForPlanKg
                })
            });
        }

        [HttpGet("summary/{year}/{month}")]
        public async Task<ActionResult> GetSummary(int year, int month)
        {
            var monthlyPlan = await _context.MonthlyProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            var detailStocks = await _context.DetailStocks
                .Include(s => s.Detail)
                .ToListAsync();

            var materialStocks = await _context.MaterialStocks
                .Include(s => s.Material)
                .ToListAsync();

            var generated = await _context.GeneratedProductionPlans
                .Include(p => p.Items)
                .Where(p => p.Year == year && p.Month == month)
                .OrderByDescending(p => p.GeneratedAt)
                .FirstOrDefaultAsync();

            var materialReport = await _executionService.BuildMaterialReportAsync(year, month);

            var totalDemand = monthlyPlan?.Items?.Sum(i => i.Quantity) ?? 0;
            var onStock = detailStocks.Sum(s => s.CurrentQuantity);
            var generatedPieces = generated?.Items?.Sum(i => i.PlannedQuantity) ?? 0;
            var unmetDemand = Math.Max(0, totalDemand - onStock - generatedPieces);

            return Ok(new
            {
                monthlyPlanItems = monthlyPlan?.Items?.Count ?? 0,
                totalDemand,
                onStock,
                planStatus = generated?.Status ?? "None",
                confirmedAt = generated?.ConfirmedAt,
                detailStocks = detailStocks.Select(s => new
                {
                    s.DetailID,
                    DetailName = s.Detail?.DetailName,
                    s.CurrentQuantity
                }),
                materialStocks = materialStocks.GroupBy(s => s.MaterialID).Select(g => new
                {
                    MaterialID = g.Key,
                    MaterialName = g.First().Material?.MaterialName,
                    TotalKg = g.Sum(s => s.CurrentQuantity)
                }),
                generatedShifts = generated?.Items?.Count ?? 0,
                generatedPieces,
                unmetDemand,
                totalMaterialForShipmentKg = materialReport.TotalRequiredForShipmentKg,
                totalMaterialForPlanKg = materialReport.TotalRequiredForPlanKg,
                materialShortages = materialReport.Materials
                    .Where(m => m.ShortageForShipmentKg > 0)
                    .Select(m => new
                    {
                        m.MaterialName,
                        requiredForShipmentKg = m.RequiredForShipmentKg,
                        requiredForPlanKg = m.RequiredForPlanKg,
                        m.AvailableKg,
                        shortageForShipmentKg = m.ShortageForShipmentKg,
                        shortageForPlanKg = m.ShortageForPlanKg,
                        usedForDetails = m.Details
                    })
            });
        }
    }
}
