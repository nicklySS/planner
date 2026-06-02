using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Data;
using ProductionApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialInventoryController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public MaterialInventoryController(ProductionDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить остаток материала по ID материала и размера
        /// </summary>
        [HttpGet("stock/{materialId}/{sizeId}")]
        public async Task<ActionResult> GetMaterialStock(int materialId, int sizeId)
        {
            var stock = await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.MaterialSize)
                .FirstOrDefaultAsync(ms =>
                    ms.MaterialID == materialId && ms.MaterialSizeID == sizeId);

            if (stock == null)
                return NotFound("Material stock not found");

            return Ok(new
            {
                stock.MaterialStockID,
                stock.MaterialID,
                stock.MaterialSizeID,
                Material = stock.Material?.MaterialName,
                Size = stock.MaterialSize?.SizeValue,
                Unit = stock.MaterialSize?.Unit,
                CurrentQuantity = stock.CurrentQuantity,
                ReceivedQuantity = stock.ReceivedQuantity,
                UsedQuantity = stock.UsedQuantity,
                LastUpdated = stock.LastUpdated
            });
        }

        /// <summary>
        /// Получить остаток материала по ID остатка
        /// </summary>
        [HttpGet("stock-by-id/{stockId}")]
        public async Task<ActionResult> GetMaterialStockById(int stockId)
        {
            var stock = await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.MaterialSize)
                .FirstOrDefaultAsync(ms => ms.MaterialStockID == stockId);

            if (stock == null)
                return NotFound("Material stock not found");

            return Ok(new
            {
                stock.MaterialStockID,
                stock.MaterialID,
                stock.MaterialSizeID,
                Material = stock.Material?.MaterialName,
                Size = stock.MaterialSize?.SizeValue,
                Unit = stock.MaterialSize?.Unit,
                CurrentQuantity = stock.CurrentQuantity,
                ReceivedQuantity = stock.ReceivedQuantity,
                UsedQuantity = stock.UsedQuantity,
                LastUpdated = stock.LastUpdated
            });
        }

        /// <summary>
        /// Получить все остатки материалов
        /// </summary>
        [HttpGet("stocks")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllStocks()
        {
            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.MaterialSize)
                .Select(s => new
                {
                    s.MaterialStockID,
                    Material = s.Material!.MaterialName,
                    Size = s.MaterialSize!.SizeValue,
                    Unit = s.MaterialSize!.Unit,
                    s.CurrentQuantity,
                    s.ReceivedQuantity,
                    s.UsedQuantity,
                    s.LastUpdated
                })
                .ToListAsync();

            return Ok(stocks);
        }

        /// <summary>
        /// Приход материала (поступление на склад)
        /// </summary>
        [HttpPost("receipt")]
        public async Task<ActionResult> AddMaterialReceipt([FromBody] MaterialReceiptDto dto)
        {
            var stock = await _context.MaterialStocks
                .FirstOrDefaultAsync(ms =>
                    ms.MaterialID == dto.MaterialId && ms.MaterialSizeID == dto.SizeId);

            // Если остатка нет, создаём новый
            if (stock == null)
            {
                stock = new MaterialStock
                {
                    MaterialID = dto.MaterialId,
                    MaterialSizeID = dto.SizeId,
                    CurrentQuantity = 0,
                    ReceivedQuantity = 0,
                    UsedQuantity = 0,
                    LastUpdated = DateTime.Now
                };
                _context.MaterialStocks.Add(stock);
            }

            // Обновляем остаток
            stock.CurrentQuantity += dto.Quantity;
            stock.ReceivedQuantity += dto.Quantity;
            stock.LastUpdated = DateTime.Now;

            // Создаём запись в журнал операций
            var transaction = new MaterialTransaction
            {
                MaterialID = dto.MaterialId,
                MaterialSizeID = dto.SizeId,
                Quantity = dto.Quantity,
                TransactionType = "Receipt",
                TransactionDate = DateTime.Now,
                Description = dto.Description,
                DocumentNumber = dto.DocumentNumber
            };

            _context.MaterialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Material receipt added successfully",
                Stock = new
                {
                    stock.MaterialStockID,
                    CurrentQuantity = stock.CurrentQuantity,
                    ReceivedQuantity = stock.ReceivedQuantity,
                    UsedQuantity = stock.UsedQuantity
                }
            });
        }

        /// <summary>
        /// Расход материала (уход со склада/использование)
        /// </summary>
        [HttpPost("consumption")]
        public async Task<ActionResult> AddMaterialConsumption([FromBody] MaterialConsumptionDto dto)
        {
            var stock = await _context.MaterialStocks
                .FirstOrDefaultAsync(ms =>
                    ms.MaterialID == dto.MaterialId && ms.MaterialSizeID == dto.SizeId);

            // Проверяем, существует ли остаток и достаточно ли материала
            if (stock == null || stock.CurrentQuantity < dto.Quantity)
                return BadRequest("Insufficient material quantity in stock");

            // Обновляем остаток
            stock.CurrentQuantity -= dto.Quantity;
            stock.UsedQuantity += dto.Quantity;
            stock.LastUpdated = DateTime.Now;

            // Создаём запись в журнал операций (с отрицательным количеством)
            var transaction = new MaterialTransaction
            {
                MaterialID = dto.MaterialId,
                MaterialSizeID = dto.SizeId,
                Quantity = -dto.Quantity,
                TransactionType = "Consumption",
                TransactionDate = DateTime.Now,
                Description = dto.Description,
                DocumentNumber = dto.DocumentNumber
            };

            _context.MaterialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Material consumption recorded successfully",
                Stock = new
                {
                    stock.MaterialStockID,
                    CurrentQuantity = stock.CurrentQuantity,
                    ReceivedQuantity = stock.ReceivedQuantity,
                    UsedQuantity = stock.UsedQuantity
                }
            });
        }

        /// <summary>
        /// Обновить остаток материала
        /// </summary>
        [HttpPut("stock/{materialStockId}")]
        public async Task<ActionResult> UpdateMaterialStock(int materialStockId, [FromBody] UpdateMaterialStockDto dto)
        {
            var stock = await _context.MaterialStocks.FindAsync(materialStockId);
            if (stock == null)
                return NotFound("Material stock not found");

            stock.CurrentQuantity = dto.CurrentQuantity;
            stock.ReceivedQuantity = dto.ReceivedQuantity;
            stock.UsedQuantity = dto.UsedQuantity;
            stock.LastUpdated = DateTime.Now;

            _context.MaterialStocks.Update(stock);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Material stock updated successfully",
                stock.MaterialStockID,
                stock.CurrentQuantity,
                stock.ReceivedQuantity,
                stock.UsedQuantity
            });
        }

        /// <summary>
        /// Удалить остаток материала
        /// </summary>
        [HttpDelete("stock/{materialStockId}")]
        public async Task<ActionResult> DeleteMaterialStock(int materialStockId)
        {
            var stock = await _context.MaterialStocks.FindAsync(materialStockId);
            if (stock == null)
                return NotFound("Material stock not found");

            _context.MaterialStocks.Remove(stock);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Material stock deleted successfully",
                DeletedStockId = materialStockId
            });
        }

        /// <summary>
        /// История операций с конкретным материалом
        /// </summary>
        [HttpGet("transactions/{materialId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetMaterialTransactions(int materialId)
        {
            var transactions = await _context.MaterialTransactions
                .Where(t => t.MaterialID == materialId)
                .Include(t => t.Material)
                .Include(t => t.MaterialSize)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.TransactionID,
                    t.MaterialID,
                    Material = t.Material!.MaterialName,
                    Size = t.MaterialSize!.SizeValue,
                    Unit = t.MaterialSize!.Unit,
                    t.Quantity,
                    t.TransactionType,
                    t.TransactionDate,
                    t.Description,
                    t.DocumentNumber
                })
                .ToListAsync();

            return Ok(transactions);
        }

        /// <summary>
        /// Полный отчёт по всем материалам (остатки, получено, использовано)
        /// </summary>
        [HttpGet("report")]
        public async Task<ActionResult<IEnumerable<object>>> GetInventoryReport()
        {
            var report = await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.MaterialSize)
                .Select(s => new
                {
                    s.MaterialStockID,
                    Material = s.Material!.MaterialName,
                    SizeValue = s.MaterialSize!.SizeValue,
                    Unit = s.MaterialSize!.Unit,
                    CurrentQuantity = s.CurrentQuantity,
                    ReceivedQuantity = s.ReceivedQuantity,
                    UsedQuantity = s.UsedQuantity,
                    Balance = s.CurrentQuantity,
                    s.LastUpdated
                })
                .OrderBy(r => r.Material)
                .ToListAsync();

            return Ok(report);
        }

        /// <summary>
        /// История всех операций (все транзакции)
        /// </summary>
        [HttpGet("all-transactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTransactions()
        {
            var transactions = await _context.MaterialTransactions
                .Include(t => t.Material)
                .Include(t => t.MaterialSize)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.TransactionID,
                    t.MaterialID,
                    Material = t.Material!.MaterialName,
                    Size = t.MaterialSize!.SizeValue,
                    Unit = t.MaterialSize!.Unit,
                    t.Quantity,
                    t.TransactionType,
                    t.TransactionDate,
                    t.Description,
                    t.DocumentNumber
                })
                .ToListAsync();

            return Ok(transactions);
        }

        /// <summary>
        /// Получить статистику по конкретному материалу (по размерам)
        /// </summary>
        [HttpGet("material-statistics/{materialId}")]
        public async Task<ActionResult<object>> GetMaterialStatistics(int materialId)
        {
            var material = await _context.Materials
                .Include(m => m.MaterialMaterialSizes)
                .FirstOrDefaultAsync(m => m.MaterialID == materialId);

            if (material == null)
                return NotFound("Material not found");

            var stocks = await _context.MaterialStocks
                .Where(s => s.MaterialID == materialId)
                .Include(s => s.MaterialSize)
                .ToListAsync();

            var statistics = new
            {
                Material = material.MaterialName,
                Sizes = stocks.Select(s => new
                {
                    Size = s.MaterialSize?.SizeValue,
                    Unit = s.MaterialSize?.Unit,
                    Current = s.CurrentQuantity,
                    Received = s.ReceivedQuantity,
                    Used = s.UsedQuantity
                })
            };

            return Ok(statistics);
        }
    }

    /// <summary>
    /// DTO для прихода материала
    /// </summary>
    public class MaterialReceiptDto
    {
        public int MaterialId { get; set; }
        public int SizeId { get; set; }
        public decimal Quantity { get; set; }
        public string? Description { get; set; }
        public int? DocumentNumber { get; set; }
    }

    /// <summary>
    /// DTO для расхода материала
    /// </summary>
    public class MaterialConsumptionDto
    {
        public int MaterialId { get; set; }
        public int SizeId { get; set; }
        public decimal Quantity { get; set; }
        public string? Description { get; set; }
        public int? DocumentNumber { get; set; }
    }

    /// <summary>
    /// DTO для обновления остатка материала
    /// </summary>
    public class UpdateMaterialStockDto
    {
        public int MaterialStockID { get; set; }
        public int MaterialID { get; set; }
        public int MaterialSizeID { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
