using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Services
{
    /// <summary>
    /// Остаток MaterialStocks.CurrentQuantity хранится в штуках (заготовках) конкретной размерности.
    /// Количество материала в единицах размера = штуки × SizeValue.
    /// </summary>
    public static class MaterialStockHelper
    {
        public static decimal GetMaterialAmount(decimal pieceCount, MaterialSize? size)
        {
            if (size == null || size.SizeValue <= 0)
                return pieceCount;
            return pieceCount * size.SizeValue;
        }

        public static decimal GetMaterialAmount(MaterialStock stock, MaterialSize? size)
            => GetMaterialAmount(stock.CurrentQuantity, size);

        public static async Task<Dictionary<int, decimal>> GetAvailableAmountByMaterialAsync(
            ProductionDbContext context)
        {
            var stocks = await context.MaterialStocks
                .Include(s => s.MaterialSize)
                .ToListAsync();

            return stocks
                .Where(s => s.MaterialSize != null)
                .GroupBy(s => s.MaterialID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => GetMaterialAmount(s, s.MaterialSize)));
        }

        public static string FormatSizeLabel(MaterialSize? size)
        {
            if (size == null) return "-";
            return $"по {size.SizeValue} {size.Unit}";
        }
    }
}
