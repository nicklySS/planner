using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Services
{
    public class ProductionPlanningService
    {
        private readonly ProductionDbContext _context;
        private static readonly string[] ShiftOrder = { "1я", "2я" };
        private const decimal DefaultShiftHours = 8m;

        public ProductionPlanningService(ProductionDbContext context)
        {
            _context = context;
        }

        public async Task<GeneratedProductionPlan> GeneratePlanAsync(int year, int month)
        {
            var monthlyPlan = await _context.MonthlyProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            if (monthlyPlan?.Items == null || !monthlyPlan.Items.Any())
                throw new InvalidOperationException("Месячный план не найден или пуст. Сначала заполните план отгрузок.");

            var details = await _context.Details.ToListAsync();
            var allEquipment = await _context.Equipment.ToListAsync();
            var detailOps = await _context.DetailOperations
                .Include(o => o.Equipment)
                .ToListAsync();
            var reconfigTimes = await _context.DetailToDetailReconfigurationTimes.ToListAsync();

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var equipmentTimesheets = await _context.EquipmentTimeSheet
                .Where(ets => ets.WorkDate >= startDate && ets.WorkDate <= endDate)
                .ToListAsync();

            NormalizeShiftCodes(equipmentTimesheets);

            var materialStocks = await _context.MaterialStocks.ToListAsync();
            var detailStocks = await _context.DetailStocks.ToListAsync();

            var virtualMaterialKg = materialStocks
                .GroupBy(s => s.MaterialID)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.CurrentQuantity));

            // Начальный остаток на складе — не меняется в ходе симуляции
            var initialDetailStock = detailStocks.ToDictionary(s => s.DetailID, s => s.CurrentQuantity);

            foreach (var d in details)
            {
                if (!initialDetailStock.ContainsKey(d.DetailID))
                    initialDetailStock[d.DetailID] = 0;
            }

            var demands = BuildDemands(monthlyPlan.Items);
            var simulatedProduction = new Dictionary<int, int>();
            var machineState = new Dictionary<int, int?>();

            var generatedItems = new List<GeneratedProductionPlanItem>();
            var shifts = BuildShiftSequence(startDate, endDate, DateTime.Today);

            foreach (var (workDate, shiftCode) in shifts)
            {
                var availableEquipment = GetAvailableEquipment(
                    allEquipment, equipmentTimesheets, workDate, shiftCode);

                var overdueDetails = GetOverdueDetailIds(demands, initialDetailStock, simulatedProduction, workDate, shiftCode);

                foreach (var equipmentId in availableEquipment.Keys)
                {
                    var hours = availableEquipment[equipmentId];
                    var shiftMinutes = (double)(hours * 60m);

                    var possibleDetails = detailOps
                        .Where(o => o.EquipmentID == equipmentId && o.NormPerShift > 0)
                        .Select(o => o.DetailID)
                        .Distinct()
                        .ToList();

                    if (!possibleDetails.Any())
                        continue;

                    var candidates = BuildCandidateList(
                        demands, initialDetailStock, simulatedProduction,
                        possibleDetails, overdueDetails, workDate, shiftCode);

                    if (!candidates.Any())
                        continue;

                    int? selectedDetail = null;
                    DetailOperation? selectedOp = null;

                    foreach (var candidate in candidates)
                    {
                        var op = detailOps.FirstOrDefault(o =>
                            o.EquipmentID == equipmentId && o.DetailID == candidate.DetailID);
                        if (op == null) continue;

                        if (!HasMaterial(candidate.DetailID, 1, details, virtualMaterialKg))
                            continue;

                        selectedDetail = candidate.DetailID;
                        selectedOp = op;
                        break;
                    }

                    if (selectedDetail == null || selectedOp == null)
                        continue;

                    var norm = selectedOp.NormPerShift!.Value;
                    var capacityRatio = (decimal)(shiftMinutes / (double)(DefaultShiftHours * 60m));
                    var maxByTime = (int)Math.Floor(norm * capacityRatio);

                    if (machineState.TryGetValue(equipmentId, out var prevDetail) &&
                        prevDetail.HasValue && prevDetail.Value != selectedDetail.Value)
                    {
                        var reconfig = reconfigTimes.FirstOrDefault(r =>
                            r.EquipmentID == equipmentId &&
                            r.FromDetailID == prevDetail.Value &&
                            r.ToDetailID == selectedDetail.Value);

                        if (reconfig != null && shiftMinutes > 0)
                        {
                            var lostPieces = (int)Math.Ceiling(norm * (reconfig.ReconfigurationMinutes / shiftMinutes));
                            maxByTime = Math.Max(0, maxByTime - lostPieces);
                        }
                    }
                    else if (!machineState.ContainsKey(equipmentId) || machineState[equipmentId] == null)
                    {
                        if (selectedOp.SetupPercentage.HasValue && selectedOp.SetupPercentage > 0)
                        {
                            maxByTime = (int)Math.Floor(maxByTime * (1 - selectedOp.SetupPercentage.Value / 100m));
                        }
                    }

                    if (maxByTime <= 0)
                    {
                        machineState[equipmentId] = selectedDetail;
                        continue;
                    }

                    var stillNeeded = GetStillNeeded(selectedDetail.Value, demands, initialDetailStock, simulatedProduction);
                    var maxByMaterial = GetMaxProducibleByMaterial(selectedDetail.Value, details, virtualMaterialKg);
                    var qty = Math.Min(Math.Min(maxByTime, stillNeeded), maxByMaterial);

                    if (qty <= 0)
                    {
                        machineState[equipmentId] = selectedDetail;
                        continue;
                    }

                    ConsumeMaterial(selectedDetail.Value, qty, details, virtualMaterialKg);
                    if (!simulatedProduction.ContainsKey(selectedDetail.Value))
                        simulatedProduction[selectedDetail.Value] = 0;
                    simulatedProduction[selectedDetail.Value] += qty;

                    var isOverdue = overdueDetails.Contains(selectedDetail.Value);

                    generatedItems.Add(new GeneratedProductionPlanItem
                    {
                        WorkDate = workDate,
                        ShiftCode = shiftCode,
                        EquipmentID = equipmentId,
                        DetailID = selectedDetail.Value,
                        PlannedQuantity = qty,
                        IsOverdue = isOverdue,
                        Notes = isOverdue ? "Просроченная отгрузка" : null
                    });

                    machineState[equipmentId] = selectedDetail;
                }
            }

            var confirmed = await _context.GeneratedProductionPlans
                .AnyAsync(p => p.Year == year && p.Month == month && p.Status == "Confirmed");

            if (confirmed)
                throw new InvalidOperationException("План уже подтверждён. Сначала нельзя сформировать новый — подтверждённый план защищён.");

            var existing = await _context.GeneratedProductionPlans
                .Where(p => p.Year == year && p.Month == month)
                .ToListAsync();

            if (existing.Any())
            {
                _context.GeneratedProductionPlans.RemoveRange(existing);
                await _context.SaveChangesAsync();
            }

            var generatedPlan = new GeneratedProductionPlan
            {
                Year = year,
                Month = month,
                GeneratedAt = DateTime.UtcNow,
                Status = "Draft",
                Items = generatedItems
            };

            _context.GeneratedProductionPlans.Add(generatedPlan);
            await _context.SaveChangesAsync();

            return await _context.GeneratedProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Equipment)
                .FirstAsync(p => p.GeneratedPlanID == generatedPlan.GeneratedPlanID);
        }

        private static void NormalizeShiftCodes(List<EquipmentTimeSheet> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.ShiftCode == "1") entry.ShiftCode = "1я";
                if (entry.ShiftCode == "2") entry.ShiftCode = "2я";
            }
        }

        private static List<(DateTime WorkDate, string ShiftCode)> BuildShiftSequence(
            DateTime start, DateTime end, DateTime fromDate)
        {
            var result = new List<(DateTime, string)>();
            var effectiveStart = fromDate.Date > start.Date ? fromDate.Date : start.Date;

            for (var date = effectiveStart; date <= end; date = date.AddDays(1))
            {
                var shiftsForDay = date == effectiveStart
                    ? ShiftOrder.SkipWhile(s => s != GetCurrentOrNextShift(fromDate))
                    : ShiftOrder;

                foreach (var shift in shiftsForDay)
                    result.Add((date, shift));
            }
            return result;
        }

        private static string GetCurrentOrNextShift(DateTime now)
        {
            // До 14:00 — с 1-й смены, после — со 2-й (упрощённая модель)
            return now.Hour < 14 ? "1я" : "2я";
        }

        private static Dictionary<int, decimal> GetAvailableEquipment(
            List<Equipment> allEquipment,
            List<EquipmentTimeSheet> timesheets,
            DateTime workDate,
            string shiftCode)
        {
            var result = new Dictionary<int, decimal>();

            foreach (var equipment in allEquipment)
            {
                var entry = timesheets.FirstOrDefault(ets =>
                    ets.EquipmentID == equipment.EquipmentID &&
                    ets.WorkDate.Date == workDate.Date &&
                    ets.ShiftCode == shiftCode);

                if (entry != null)
                {
                    if (entry.DayType == "Work" && entry.HoursWorked > 0)
                        result[equipment.EquipmentID] = entry.HoursWorked ?? DefaultShiftHours;
                }
                else
                {
                    // Нет записи в табеле — считаем станок рабочим в Пн–Пт (8 ч)
                    var dayOfWeek = workDate.DayOfWeek;
                    if (dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday)
                        result[equipment.EquipmentID] = DefaultShiftHours;
                }
            }

            return result;
        }

        private class DemandItem
        {
            public int DetailID { get; set; }
            public int Quantity { get; set; }
            public DateTime ShipmentDate { get; set; }
            public DateTime DeadlineDate { get; set; }
            public string DeadlineShift { get; set; } = "2я";
        }

        private class Candidate
        {
            public int DetailID { get; set; }
            public int Priority { get; set; }
            public DateTime DeadlineDate { get; set; }
            public string DeadlineShift { get; set; } = "2я";
        }

        private static List<DemandItem> BuildDemands(IEnumerable<MonthlyProductionPlanItem> items)
        {
            return items.Select(i => new DemandItem
            {
                DetailID = i.DetailID,
                Quantity = i.Quantity,
                ShipmentDate = i.ShipmentDate.Date,
                DeadlineDate = i.ShipmentDate.Date.AddDays(-1),
                DeadlineShift = "2я"
            }).ToList();
        }

        private static int GetStillNeeded(
            int detailId,
            List<DemandItem> demands,
            Dictionary<int, int> initialDetailStock,
            Dictionary<int, int> simulatedProduction)
        {
            var totalDemand = demands.Where(d => d.DetailID == detailId).Sum(d => d.Quantity);
            var onStock = initialDetailStock.GetValueOrDefault(detailId, 0);
            var produced = simulatedProduction.GetValueOrDefault(detailId, 0);
            return Math.Max(0, totalDemand - onStock - produced);
        }

        private static HashSet<int> GetOverdueDetailIds(
            List<DemandItem> demands,
            Dictionary<int, int> detailStock,
            Dictionary<int, int> simulatedProduction,
            DateTime workDate,
            string shiftCode)
        {
            var overdue = new HashSet<int>();
            var currentOrder = ShiftOrder.ToList().IndexOf(shiftCode);

            foreach (var demand in demands)
            {
                var allocated = AllocateToDemand(demand, detailStock, simulatedProduction);
                if (allocated >= demand.Quantity)
                    continue;

                var deadlineOrder = ShiftOrder.ToList().IndexOf(demand.DeadlineShift);
                var isPastDeadline =
                    workDate.Date > demand.DeadlineDate.Date ||
                    (workDate.Date == demand.DeadlineDate.Date && currentOrder > deadlineOrder);

                if (isPastDeadline)
                    overdue.Add(demand.DetailID);
            }

            return overdue;
        }

        private static int AllocateToDemand(
            DemandItem demand,
            Dictionary<int, int> initialDetailStock,
            Dictionary<int, int> simulatedProduction)
        {
            var totalAvailable = initialDetailStock.GetValueOrDefault(demand.DetailID, 0)
                + simulatedProduction.GetValueOrDefault(demand.DetailID, 0);
            return Math.Min(demand.Quantity, totalAvailable);
        }

        private static List<Candidate> BuildCandidateList(
            List<DemandItem> demands,
            Dictionary<int, int> detailStock,
            Dictionary<int, int> simulatedProduction,
            List<int> possibleDetails,
            HashSet<int> overdueDetails,
            DateTime workDate,
            string shiftCode)
        {
            var candidates = new List<Candidate>();
            var currentOrder = ShiftOrder.ToList().IndexOf(shiftCode);

            foreach (var detailId in possibleDetails)
            {
                var stillNeeded = GetStillNeeded(detailId, demands, detailStock, simulatedProduction);
                if (stillNeeded <= 0)
                    continue;

                var detailDemands = demands.Where(d => d.DetailID == detailId).ToList();
                if (!detailDemands.Any())
                    continue;

                var earliest = detailDemands
                    .OrderBy(d => d.DeadlineDate)
                    .ThenBy(d => ShiftOrder.ToList().IndexOf(d.DeadlineShift))
                    .First();

                var isOverdue = overdueDetails.Contains(detailId);
                var daysToDeadline = (earliest.DeadlineDate.Date - workDate.Date).Days;
                var shiftPenalty = ShiftOrder.ToList().IndexOf(earliest.DeadlineShift);

                var priority = isOverdue
                    ? -10000 + daysToDeadline
                    : daysToDeadline * 10 + shiftPenalty;

                if (workDate.Date == earliest.DeadlineDate.Date && currentOrder > shiftPenalty)
                    priority = -5000;

                candidates.Add(new Candidate
                {
                    DetailID = detailId,
                    Priority = priority,
                    DeadlineDate = earliest.DeadlineDate,
                    DeadlineShift = earliest.DeadlineShift
                });
            }

            return candidates
                .OrderBy(c => c.Priority)
                .ThenBy(c => c.DeadlineDate)
                .ThenBy(c => ShiftOrder.ToList().IndexOf(c.DeadlineShift))
                .ToList();
        }

        private static bool HasMaterial(
            int detailId, int qty, List<Detail> details, Dictionary<int, decimal> materialKg)
        {
            return GetMaxProducibleByMaterial(detailId, details, materialKg) >= qty;
        }

        private static int GetMaxProducibleByMaterial(
            int detailId, List<Detail> details, Dictionary<int, decimal> materialKg)
        {
            var detail = details.FirstOrDefault(d => d.DetailID == detailId);
            if (detail?.MainMaterial == null || detail.ConsumptionRate == null || detail.ConsumptionRate <= 0)
                return int.MaxValue;

            var availableKg = materialKg.GetValueOrDefault(detail.MainMaterial.Value, 0);
            return (int)Math.Floor(availableKg / detail.ConsumptionRate.Value);
        }

        private static void ConsumeMaterial(
            int detailId, int qty, List<Detail> details, Dictionary<int, decimal> materialKg)
        {
            var detail = details.FirstOrDefault(d => d.DetailID == detailId);
            if (detail?.MainMaterial == null || detail.ConsumptionRate == null || detail.ConsumptionRate <= 0)
                return;

            var consumed = detail.ConsumptionRate.Value * qty;
            var materialId = detail.MainMaterial.Value;
            materialKg[materialId] = Math.Max(0, materialKg.GetValueOrDefault(materialId, 0) - consumed);
        }
    }
}
