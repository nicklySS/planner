using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Services
{
    public class PlannerExcelService
    {
        static PlannerExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ImportResult> ImportShipmentPlanAsync(IFormFile file, int year, int month, ProductionDbContext context)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Файл не выбран");

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Поддерживаются только файлы Excel (.xlsx, .xls)");
            }

            using var package = new ExcelPackage(file.OpenReadStream());
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                throw new InvalidOperationException("Файл Excel не содержит листов");

            var headerMap = ReadHeaderMap(worksheet);
            if (headerMap == null)
                throw new InvalidOperationException("В файле не найден заголовок. Ожидаются колонки: Деталь, Дата, Количество");

            var rows = new List<ShipmentPlanImportRow>();
            var details = await context.Details.AsNoTracking().ToListAsync();
            var lastRow = worksheet.Dimension?.End.Row ?? 1;

            for (var rowIndex = 2; rowIndex <= lastRow; rowIndex++)
            {
                var detailCell = worksheet.Cells[rowIndex, headerMap["detail"]].Text?.Trim();
                var dateCell = worksheet.Cells[rowIndex, headerMap["date"]].Value;
                var qtyCell = worksheet.Cells[rowIndex, headerMap["quantity"]].Value;

                if (string.IsNullOrWhiteSpace(detailCell) && dateCell == null && qtyCell == null)
                    continue;

                var shipmentDate = ParseDate(dateCell);
                var quantity = ParseQuantity(qtyCell);
                var detail = FindDetail(details, detailCell);

                if (detail == null || quantity <= 0 || shipmentDate == DateTime.MinValue)
                    continue;

                rows.Add(new ShipmentPlanImportRow
                {
                    DetailID = detail.DetailID,
                    Quantity = quantity,
                    ShipmentDate = shipmentDate.Date,
                    Notes = null
                });
            }

            if (rows.Count == 0)
                throw new InvalidOperationException("В файле не найдено ни одной валидной строки");

            var plan = await context.MonthlyProductionPlans
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
                context.MonthlyProductionPlans.Add(plan);
                await context.SaveChangesAsync();
            }

            if (plan.Items != null && plan.Items.Any())
                context.MonthlyProductionPlanItems.RemoveRange(plan.Items);

            foreach (var row in rows)
            {
                context.MonthlyProductionPlanItems.Add(new MonthlyProductionPlanItem
                {
                    PlanID = plan.PlanID,
                    DetailID = row.DetailID,
                    Quantity = row.Quantity,
                    ShipmentDate = row.ShipmentDate,
                    Notes = row.Notes
                });
            }

            await context.SaveChangesAsync();
            return new ImportResult
            {
                ImportedRows = rows.Count,
                PlanID = plan.PlanID
            };
        }

        public async Task<MemoryStream> ExportShipmentPlanAsync(int year, int month, ProductionDbContext context)
        {
            var plan = await context.MonthlyProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Year == year && p.Month == month);

            var items = (plan?.Items ?? Enumerable.Empty<MonthlyProductionPlanItem>())
                .OrderBy(i => i.ShipmentDate)
                .ThenBy(i => i.DetailID)
                .ToList();

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("План отгрузок");
                worksheet.Cells[1, 1].Value = "Деталь";
                worksheet.Cells[1, 2].Value = "Дата";
                worksheet.Cells[1, 3].Value = "Количество";
                worksheet.Cells[1, 4].Value = "Примечание";

                for (var i = 0; i < items.Count; i++)
                {
                    var row = i + 2;
                    worksheet.Cells[row, 1].Value = BuildDetailDisplayName(items[i].Detail, items[i].DetailID);
                    worksheet.Cells[row, 2].Value = items[i].ShipmentDate;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[row, 3].Value = items[i].Quantity;
                    worksheet.Cells[row, 4].Value = items[i].Notes;
                }

                using (var headerRange = worksheet.Cells[1, 1, 1, 4])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 78, 121));
                    headerRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    headerRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                using (var tableRange = worksheet.Cells[1, 1, items.Count + 1, 4])
                {
                    tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                worksheet.Cells[2, 1, items.Count + 1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[2, 1, items.Count + 1, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(242, 242, 242));
                worksheet.Cells[2, 1, items.Count + 1, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
                worksheet.Cells[1, 1, items.Count + 1, 4].AutoFitColumns();
                worksheet.View.FreezePanes(2, 1);
                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        public async Task<MemoryStream> ExportShiftPlanAsync(int year, int month, ProductionDbContext context)
        {
            var plan = await context.GeneratedProductionPlans
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Detail)
                .Include(p => p.Items!)
                    .ThenInclude(i => i.Equipment)
                .AsNoTracking()
                .Where(p => p.Year == year && p.Month == month)
                .OrderByDescending(p => p.GeneratedAt)
                .FirstOrDefaultAsync();

            var items = (plan?.Items ?? Enumerable.Empty<GeneratedProductionPlanItem>())
                .OrderBy(i => i.WorkDate)
                .ThenBy(i => i.ShiftCode)
                .ThenBy(i => i.EquipmentID)
                .ThenBy(i => i.DetailID)
                .ToList();

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                if (items.Count == 0)
                {
                    var worksheet = package.Workbook.Worksheets.Add("План смен");
                    worksheet.Cells[1, 1].Value = "Нет данных";
                    package.Save();
                    stream.Position = 0;
                    return stream;
                }

                var grouped = items.GroupBy(i => i.EquipmentID).ToList();
                for (var index = 0; index < grouped.Count; index++)
                {
                    var equipmentGroup = grouped[index];
                    var equipmentName = equipmentGroup.First().Equipment?.EquipmentName ?? $"Станок {index + 1}";
                    var sheetName = SanitizeSheetName(equipmentName, index + 1);
                    var worksheet = package.Workbook.Worksheets.Add(sheetName);

                    worksheet.Cells[1, 1].Value = "Дата";
                    worksheet.Cells[1, 2].Value = "Смена";
                    worksheet.Cells[1, 3].Value = "Станок";
                    worksheet.Cells[1, 4].Value = "Деталь";
                    worksheet.Cells[1, 5].Value = "Количество";
                    worksheet.Cells[1, 6].Value = "Просрочка";
                    worksheet.Cells[1, 7].Value = "Примечание";

                    var rows = equipmentGroup.ToList();
                    for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                    {
                        var row = rowIndex + 2;
                        worksheet.Cells[row, 1].Value = rows[rowIndex].WorkDate;
                        worksheet.Cells[row, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                        worksheet.Cells[row, 2].Value = rows[rowIndex].ShiftCode;
                        worksheet.Cells[row, 3].Value = rows[rowIndex].Equipment?.EquipmentName;
                        worksheet.Cells[row, 4].Value = BuildDetailDisplayName(rows[rowIndex].Detail, rows[rowIndex].DetailID);
                        worksheet.Cells[row, 5].Value = rows[rowIndex].PlannedQuantity;
                        worksheet.Cells[row, 6].Value = rows[rowIndex].IsOverdue ? "Да" : "Нет";
                        worksheet.Cells[row, 7].Value = rows[rowIndex].Notes;
                    }

                    using (var headerRange = worksheet.Cells[1, 1, 1, 7])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 78, 121));
                        headerRange.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        headerRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    }

                    using (var tableRange = worksheet.Cells[1, 1, rows.Count + 1, 7])
                    {
                        tableRange.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        tableRange.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    worksheet.Cells[2, 1, rows.Count + 1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[2, 1, rows.Count + 1, 7].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(250, 250, 250));
                    worksheet.Cells[1, 1, rows.Count + 1, 7].AutoFitColumns();
                    worksheet.View.FreezePanes(2, 1);
                }

                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        private static Dictionary<string, int>? ReadHeaderMap(ExcelWorksheet worksheet)
        {
            if (worksheet.Dimension == null)
                return null;

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var header = worksheet.Cells[1, col].Text?.Trim();
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var normalized = NormalizeHeader(header);
                if (!string.IsNullOrWhiteSpace(normalized))
                    map[normalized] = col;
            }

            return map.ContainsKey("detail") && map.ContainsKey("date") && map.ContainsKey("quantity")
                ? map
                : null;
        }

        private static string NormalizeHeader(string value)
        {
            var normalized = value.ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);

            if (normalized.Contains("деталь") || normalized.Contains("detail"))
                return "detail";
            if (normalized.Contains("дата") || normalized.Contains("date"))
                return "date";
            if (normalized.Contains("количество") || normalized.Contains("quantity") || normalized.Contains("qty") || normalized.Contains("amount"))
                return "quantity";

            return normalized;
        }

        private static Detail? FindDetail(IEnumerable<Detail> details, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmedValue = value.Trim();
            var (nameCandidate, codeCandidate) = ParseDetailReference(trimmedValue);

            return details.FirstOrDefault(d =>
                string.Equals(d.DetailName, trimmedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.DetailName, nameCandidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.DetailCode, trimmedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.DetailCode, codeCandidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.DetailShortCode, trimmedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.DetailShortCode, codeCandidate, StringComparison.OrdinalIgnoreCase));
        }

        private static (string? Name, string? Code) ParseDetailReference(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (null, null);

            var startIndex = value.IndexOf('(');
            var endIndex = value.LastIndexOf(')');
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var name = value.Substring(0, startIndex).Trim();
                var code = value.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                return (string.IsNullOrWhiteSpace(name) ? null : name,
                    string.IsNullOrWhiteSpace(code) ? null : code);
            }

            return (value, null);
        }

        private static int ParseQuantity(object? value)
        {
            if (value == null)
                return 0;

            if (value is int intValue)
                return intValue;
            if (value is double doubleValue)
                return (int)Math.Round(doubleValue);
            if (value is decimal decimalValue)
                return (int)Math.Round(decimalValue);
            if (value is float floatValue)
                return (int)Math.Round(floatValue);

            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                return (int)Math.Round(parsed);

            return 0;
        }

        private static DateTime ParseDate(object? value)
        {
            if (value == null)
                return DateTime.MinValue;

            if (value is DateTime dateValue)
                return dateValue.Date;

            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsedDate))
                return parsedDate.Date;

            if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
                return DateTime.FromOADate(serial).Date;

            return DateTime.MinValue;
        }

        private static string BuildDetailDisplayName(Detail? detail, int? detailId = null)
        {
            if (detail == null)
                return detailId?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(detail.DetailName))
                return detail.DetailCode ?? detail.DetailShortCode ?? detailId?.ToString() ?? string.Empty;

            var code = !string.IsNullOrWhiteSpace(detail.DetailCode)
                ? detail.DetailCode
                : detail.DetailShortCode;

            return string.IsNullOrWhiteSpace(code)
                ? detail.DetailName
                : $"{detail.DetailName} ({code})";
        }

        private static string SanitizeSheetName(string equipmentName, int fallbackIndex)
        {
            var safeName = string.Join(string.Empty, equipmentName
                .Where(c => c != ':' && c != '/' && c != '\\' && c != '?' && c != '*' && c != '[' && c != ']' && c != '(' && c != ')'))
                .Trim();

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = $"Станок {fallbackIndex}";

            if (safeName.Length > 31)
                safeName = safeName[..31];

            return safeName;
        }
    }

    public class ImportResult
    {
        public int ImportedRows { get; set; }
        public int PlanID { get; set; }
    }

    public class ShipmentPlanImportRow
    {
        public int DetailID { get; set; }
        public int Quantity { get; set; }
        public DateTime ShipmentDate { get; set; }
        public string? Notes { get; set; }
    }
}
