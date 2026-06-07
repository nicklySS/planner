using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Services
{
    public class ProductionPlanExecutionService
    {
        private readonly ProductionDbContext _context;

        public ProductionPlanExecutionService(ProductionDbContext context)
        {
            _context = context;
        }

        public async Task<GeneratedProductionPlan> ConfirmPlanAsync(int year, int month)
        {
            var plan = await LoadLatestPlanAsync(year, month);

            if (plan?.Items == null || !plan.Items.Any())
                throw new InvalidOperationException("Нет сгенерированного плана для подтверждения.");

            if (plan.Status == "Confirmed")
                throw new InvalidOperationException("Этот план уже подтверждён.");

            var materialStocks = await _context.MaterialStocks
                .GroupBy(s => s.MaterialID)
                .Select(g => new { MaterialID = g.Key, TotalKg = g.Sum(s => s.CurrentQuantity) })
                .ToDictionaryAsync(x => x.MaterialID, x => x.TotalKg);

            var planAnalysis = BuildMaterialAnalysisFromGenerated(plan.Items);
            foreach (var mat in planAnalysis)
            {
                mat.AvailableKg = materialStocks.GetValueOrDefault(mat.MaterialID, 0);
                mat.ShortageForPlanKg = Math.Max(0, mat.RequiredForPlanKg - mat.AvailableKg);
            }

            var shortages = planAnalysis.Where(a => a.ShortageForPlanKg > 0).ToList();
            if (shortages.Any())
            {
                var details = string.Join("; ", shortages.Select(s =>
                    $"{s.MaterialName}: не хватает {s.ShortageForPlanKg:F2} кг"));
                throw new InvalidOperationException($"Недостаточно материала для выполнения плана. {details}");
            }

            var byDetail = plan.Items
                .GroupBy(i => i.DetailID)
                .Select(g => new { DetailID = g.Key, Quantity = g.Sum(i => i.PlannedQuantity) })
                .ToList();

            foreach (var row in byDetail)
            {
                await AddDetailProductionAsync(row.DetailID, row.Quantity, plan.GeneratedPlanID);
            }

            var materialDesc = BuildMaterialConsumptionDescription(plan.GeneratedPlanID, month, year);

            foreach (var mat in planAnalysis.Where(a => a.RequiredForPlanKg > 0))
            {
                await DeductMaterialAsync(mat.MaterialID, mat.RequiredForPlanKg, materialDesc);
            }

            plan.Status = "Confirmed";
            plan.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return plan;
        }

        public async Task<GeneratedProductionPlan> CancelPlanAsync(int year, int month)
        {
            var plan = await LoadLatestPlanAsync(year, month);

            if (plan == null || plan.Status != "Confirmed")
                throw new InvalidOperationException("Нет подтверждённого плана для отмены.");

            var planId = plan.GeneratedPlanID;
            var materialDesc = BuildMaterialConsumptionDescription(planId, month, year);
            var detailDesc = BuildDetailProductionDescription(planId);

            var materialTxns = await _context.MaterialTransactions
                .Where(t => t.Description == materialDesc && t.TransactionType == "Consumption")
                .ToListAsync();

            if (materialTxns.Any())
            {
                foreach (var txn in materialTxns)
                {
                    var kg = Math.Abs(txn.Quantity);
                    await RestoreMaterialAsync(
                        txn.MaterialID,
                        txn.MaterialSizeID,
                        kg,
                        $"Отмена плана производства #{planId}");
                }
            }
            else
            {
                // Резервный откат по позициям плана, если журнал не найден
                var planAnalysis = BuildMaterialAnalysisFromGenerated(plan.Items!);
                foreach (var mat in planAnalysis.Where(a => a.RequiredForPlanKg > 0))
                {
                    await RestoreMaterialToAnyStockAsync(
                        mat.MaterialID,
                        mat.RequiredForPlanKg,
                        $"Отмена плана производства #{planId}");
                }
            }

            var detailTxns = await _context.DetailTransactions
                .Where(t => t.Description == detailDesc && t.TransactionType == "Production")
                .ToListAsync();

            if (detailTxns.Any())
            {
                foreach (var txn in detailTxns)
                {
                    await RemoveDetailProductionAsync(txn.DetailID, txn.Quantity, planId);
                }
            }
            else
            {
                var byDetail = plan.Items!
                    .GroupBy(i => i.DetailID)
                    .Select(g => new { DetailID = g.Key, Quantity = g.Sum(i => i.PlannedQuantity) });

                foreach (var row in byDetail)
                {
                    await RemoveDetailProductionAsync(row.DetailID, row.Quantity, planId);
                }
            }

            plan.Status = "Draft";
            plan.ConfirmedAt = null;
            await _context.SaveChangesAsync();

            return plan;
        }

        private static string BuildMaterialConsumptionDescription(int planId, int month, int year) =>
            $"Списание по плану производства #{planId} ({month:D2}.{year})";

        private static string BuildDetailProductionDescription(int planId) =>
            $"Выполнение плана производства #{planId}";

        public async Task<MaterialReport> BuildMaterialReportAsync(int year, int month)
        {
            var monthlyPlan = await _context.MonthlyProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                        .ThenInclude(d => d!.Material)
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            var generatedPlan = await LoadLatestPlanAsync(year, month);

            var detailStocks = await _context.DetailStocks
                .ToDictionaryAsync(s => s.DetailID, s => s.CurrentQuantity);

            var materialStocks = await _context.MaterialStocks
                .GroupBy(s => s.MaterialID)
                .Select(g => new { MaterialID = g.Key, TotalKg = g.Sum(s => s.CurrentQuantity) })
                .ToDictionaryAsync(x => x.MaterialID, x => x.TotalKg);

            var materials = await _context.Materials
                .ToDictionaryAsync(m => m.MaterialID, m => m.MaterialName);

            var shipmentByDetail = (monthlyPlan?.Items ?? new List<MonthlyProductionPlanItem>())
                .GroupBy(i => i.DetailID)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            var generatedByDetail = (generatedPlan?.Items ?? new List<GeneratedProductionPlanItem>())
                .GroupBy(i => i.DetailID)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.PlannedQuantity));

            var allDetailIds = shipmentByDetail.Keys
                .Union(generatedByDetail.Keys)
                .Distinct()
                .ToList();

            var details = await _context.Details
                .Include(d => d.Material)
                .Where(d => allDetailIds.Contains(d.DetailID))
                .ToDictionaryAsync(d => d.DetailID);

            var detailRows = new List<DetailMaterialUsage>();

            foreach (var detailId in allDetailIds)
            {
                if (!details.TryGetValue(detailId, out var detail))
                    continue;

                var demandQty = shipmentByDetail.GetValueOrDefault(detailId, 0);
                var onStock = detailStocks.GetValueOrDefault(detailId, 0);
                var netNeededForShipment = Math.Max(0, demandQty - onStock);
                var plannedQty = generatedByDetail.GetValueOrDefault(detailId, 0);
                var rate = detail.ConsumptionRate ?? 0;

                detailRows.Add(new DetailMaterialUsage
                {
                    DetailID = detailId,
                    DetailName = detail.DetailName,
                    DemandQuantity = demandQty,
                    OnStock = onStock,
                    NetNeededForShipment = netNeededForShipment,
                    PlannedQuantity = plannedQty,
                    MaterialID = detail.MainMaterial,
                    MaterialName = detail.Material?.MaterialName,
                    ConsumptionRate = rate,
                    RequiredForShipmentKg = rate > 0 ? rate * netNeededForShipment : 0,
                    RequiredForPlanKg = rate > 0 ? rate * plannedQty : 0
                });
            }

            detailRows = detailRows.OrderBy(d => d.DetailName).ToList();

            var materialIds = detailRows
                .Where(d => d.MaterialID.HasValue)
                .Select(d => d.MaterialID!.Value)
                .Distinct()
                .ToList();

            var materialRows = materialIds.Select(materialId =>
            {
                var rows = detailRows.Where(d => d.MaterialID == materialId).ToList();
                var requiredShipment = rows.Sum(r => r.RequiredForShipmentKg);
                var requiredPlan = rows.Sum(r => r.RequiredForPlanKg);
                var available = materialStocks.GetValueOrDefault(materialId, 0);

                return new MaterialPlanAnalysis
                {
                    MaterialID = materialId,
                    MaterialName = materials.GetValueOrDefault(materialId, $"Материал #{materialId}"),
                    RequiredForShipmentKg = requiredShipment,
                    RequiredForPlanKg = requiredPlan,
                    AvailableKg = available,
                    ShortageForShipmentKg = Math.Max(0, requiredShipment - available),
                    ShortageForPlanKg = Math.Max(0, requiredPlan - available),
                    Details = rows.Select(r => r.DetailName).Distinct().ToList()
                };
            })
            .OrderBy(m => m.MaterialName)
            .ToList();

            return new MaterialReport
            {
                TotalRequiredForShipmentKg = materialRows.Sum(m => m.RequiredForShipmentKg),
                TotalRequiredForPlanKg = materialRows.Sum(m => m.RequiredForPlanKg),
                Materials = materialRows,
                ByDetail = detailRows
            };
        }

        private async Task<GeneratedProductionPlan?> LoadLatestPlanAsync(int year, int month)
        {
            return await _context.GeneratedProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                        .ThenInclude(d => d!.Material)
                .Where(p => p.Year == year && p.Month == month)
                .OrderByDescending(p => p.GeneratedAt)
                .FirstOrDefaultAsync();
        }

        private static List<MaterialPlanAnalysis> BuildMaterialAnalysisFromGenerated(
            IEnumerable<GeneratedProductionPlanItem> items)
        {
            return items
                .GroupBy(i => i.DetailID)
                .Select(g =>
                {
                    var detail = g.First().Detail;
                    var qty = g.Sum(i => i.PlannedQuantity);
                    var rate = detail?.ConsumptionRate ?? 0;
                    return new
                    {
                        MaterialID = detail?.MainMaterial,
                        MaterialName = detail?.Material?.MaterialName ?? "Материал",
                        DetailName = detail?.DetailName ?? $"Деталь #{g.Key}",
                        RequiredForPlanKg = rate > 0 ? rate * qty : 0
                    };
                })
                .Where(x => x.MaterialID.HasValue && x.RequiredForPlanKg > 0)
                .GroupBy(x => x.MaterialID!.Value)
                .Select(g => new MaterialPlanAnalysis
                {
                    MaterialID = g.Key,
                    MaterialName = g.First().MaterialName,
                    RequiredForPlanKg = g.Sum(x => x.RequiredForPlanKg),
                    Details = g.Select(x => x.DetailName).Distinct().ToList()
                })
                .ToList();
        }

        private async Task AddDetailProductionAsync(int detailId, int quantity, int planId)
        {
            var stock = await _context.DetailStocks.FirstOrDefaultAsync(s => s.DetailID == detailId);
            if (stock == null)
            {
                stock = new DetailStock
                {
                    DetailID = detailId,
                    CurrentQuantity = 0,
                    ReceivedQuantity = 0,
                    ShippedQuantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.DetailStocks.Add(stock);
            }

            stock.CurrentQuantity += quantity;
            stock.ReceivedQuantity += quantity;
            stock.LastUpdated = DateTime.UtcNow;

            _context.DetailTransactions.Add(new DetailTransaction
            {
                DetailID = detailId,
                Quantity = quantity,
                TransactionType = "Production",
                TransactionDate = DateTime.UtcNow,
                Description = BuildDetailProductionDescription(planId)
            });
        }

        private async Task RemoveDetailProductionAsync(int detailId, int quantity, int planId)
        {
            var stock = await _context.DetailStocks.FirstOrDefaultAsync(s => s.DetailID == detailId);
            if (stock == null || stock.CurrentQuantity < quantity)
            {
                var name = await _context.Details
                    .Where(d => d.DetailID == detailId)
                    .Select(d => d.DetailName)
                    .FirstOrDefaultAsync();
                throw new InvalidOperationException(
                    $"Недостаточно деталей «{name ?? detailId.ToString()}» на складе для отмены плана (нужно списать {quantity}, есть {stock?.CurrentQuantity ?? 0}).");
            }

            stock.CurrentQuantity -= quantity;
            stock.ReceivedQuantity = Math.Max(0, stock.ReceivedQuantity - quantity);
            stock.LastUpdated = DateTime.UtcNow;

            _context.DetailTransactions.Add(new DetailTransaction
            {
                DetailID = detailId,
                Quantity = -quantity,
                TransactionType = "ProductionCancel",
                TransactionDate = DateTime.UtcNow,
                Description = $"Отмена плана производства #{planId}"
            });
        }

        private async Task RestoreMaterialAsync(int materialId, int sizeId, decimal kg, string description)
        {
            var stock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialID == materialId && s.MaterialSizeID == sizeId);

            if (stock == null)
            {
                stock = new MaterialStock
                {
                    MaterialID = materialId,
                    MaterialSizeID = sizeId,
                    CurrentQuantity = 0,
                    ReceivedQuantity = 0,
                    UsedQuantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.MaterialStocks.Add(stock);
            }

            stock.CurrentQuantity += kg;
            stock.UsedQuantity = Math.Max(0, stock.UsedQuantity - kg);
            stock.LastUpdated = DateTime.UtcNow;

            _context.MaterialTransactions.Add(new MaterialTransaction
            {
                MaterialID = materialId,
                MaterialSizeID = sizeId,
                Quantity = kg,
                TransactionType = "Receipt",
                TransactionDate = DateTime.UtcNow,
                Description = description
            });
        }

        private async Task RestoreMaterialToAnyStockAsync(int materialId, decimal kg, string description)
        {
            var stock = await _context.MaterialStocks
                .Where(s => s.MaterialID == materialId)
                .OrderByDescending(s => s.CurrentQuantity)
                .FirstOrDefaultAsync();

            if (stock == null)
            {
                var sizeId = await _context.MaterialMaterialSizes
                    .Where(mms => mms.MaterialID == materialId)
                    .Select(mms => mms.MaterialSizeID)
                    .FirstOrDefaultAsync();

                if (sizeId == 0)
                    throw new InvalidOperationException($"Не найден складской размер для материала #{materialId}");

                await RestoreMaterialAsync(materialId, sizeId, kg, description);
                return;
            }

            await RestoreMaterialAsync(materialId, stock.MaterialSizeID, kg, description);
        }

        private async Task DeductMaterialAsync(int materialId, decimal kgNeeded, string description)
        {
            var stocks = await _context.MaterialStocks
                .Where(s => s.MaterialID == materialId && s.CurrentQuantity > 0)
                .OrderByDescending(s => s.CurrentQuantity)
                .ToListAsync();

            var remaining = kgNeeded;
            foreach (var stock in stocks)
            {
                if (remaining <= 0) break;

                var deduct = Math.Min(stock.CurrentQuantity, remaining);
                stock.CurrentQuantity -= deduct;
                stock.UsedQuantity += deduct;
                stock.LastUpdated = DateTime.UtcNow;

                _context.MaterialTransactions.Add(new MaterialTransaction
                {
                    MaterialID = materialId,
                    MaterialSizeID = stock.MaterialSizeID,
                    Quantity = -deduct,
                    TransactionType = "Consumption",
                    TransactionDate = DateTime.UtcNow,
                    Description = description
                });

                remaining -= deduct;
            }

            if (remaining > 0.001m)
                throw new InvalidOperationException(
                    $"Недостаточно материала (ID {materialId}), не хватает {remaining:F2} кг");
        }
    }

    public class MaterialReport
    {
        public decimal TotalRequiredForShipmentKg { get; set; }
        public decimal TotalRequiredForPlanKg { get; set; }
        public List<MaterialPlanAnalysis> Materials { get; set; } = new();
        public List<DetailMaterialUsage> ByDetail { get; set; } = new();
    }

    public class MaterialPlanAnalysis
    {
        public int MaterialID { get; set; }
        public string MaterialName { get; set; } = null!;
        public decimal RequiredForShipmentKg { get; set; }
        public decimal RequiredForPlanKg { get; set; }
        public decimal AvailableKg { get; set; }
        public decimal ShortageForShipmentKg { get; set; }
        public decimal ShortageForPlanKg { get; set; }
        public List<string> Details { get; set; } = new();
    }

    public class DetailMaterialUsage
    {
        public int DetailID { get; set; }
        public string DetailName { get; set; } = null!;
        public int DemandQuantity { get; set; }
        public int OnStock { get; set; }
        public int NetNeededForShipment { get; set; }
        public int PlannedQuantity { get; set; }
        public int? MaterialID { get; set; }
        public string? MaterialName { get; set; }
        public decimal ConsumptionRate { get; set; }
        public decimal RequiredForShipmentKg { get; set; }
        public decimal RequiredForPlanKg { get; set; }
    }
}
