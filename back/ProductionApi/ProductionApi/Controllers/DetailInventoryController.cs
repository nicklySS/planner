using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionApi.Auth;
using ProductionApi.Data;
using ProductionApi.Models;

namespace ProductionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetailInventoryController : ControllerBase
    {
        private readonly ProductionDbContext _context;

        public DetailInventoryController(ProductionDbContext context)
        {
            _context = context;
        }

        [HttpGet("stocks")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllStocks()
        {
            var stocks = await _context.DetailStocks
                .Include(s => s.Detail)
                .Select(s => new
                {
                    s.DetailStockID,
                    s.DetailID,
                    DetailName = s.Detail!.DetailName,
                    DetailCode = s.Detail.DetailCode,
                    s.CurrentQuantity,
                    s.ReceivedQuantity,
                    s.ShippedQuantity,
                    s.LastUpdated
                })
                .ToListAsync();

            return Ok(stocks);
        }

        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllTransactions([FromQuery] int? detailId = null)
        {
            var query = _context.DetailTransactions
                .Include(t => t.Detail)
                .AsQueryable();

            if (detailId.HasValue)
                query = query.Where(t => t.DetailID == detailId.Value);

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.DetailTransactionID,
                    t.DetailID,
                    DetailName = t.Detail!.DetailName,
                    t.Quantity,
                    t.TransactionType,
                    t.TransactionDate,
                    t.Description,
                    t.DocumentNumber
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [AdminWrite]
        [HttpPost("receipt")]
        public async Task<ActionResult> AddReceipt([FromBody] DetailMovementDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest("Количество должно быть больше 0");

            var stock = await _context.DetailStocks
                .FirstOrDefaultAsync(s => s.DetailID == dto.DetailId);

            if (stock == null)
            {
                stock = new DetailStock
                {
                    DetailID = dto.DetailId,
                    CurrentQuantity = 0,
                    ReceivedQuantity = 0,
                    ShippedQuantity = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.DetailStocks.Add(stock);
            }

            stock.CurrentQuantity += dto.Quantity;
            stock.ReceivedQuantity += dto.Quantity;
            stock.LastUpdated = DateTime.UtcNow;

            _context.DetailTransactions.Add(new DetailTransaction
            {
                DetailID = dto.DetailId,
                Quantity = dto.Quantity,
                TransactionType = "Receipt",
                TransactionDate = DateTime.UtcNow,
                Description = dto.Description,
                DocumentNumber = dto.DocumentNumber
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Приход зарегистрирован", stock.CurrentQuantity });
        }

        [AdminWrite]
        [HttpPost("shipment")]
        public async Task<ActionResult> AddShipment([FromBody] DetailMovementDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest("Количество должно быть больше 0");

            var stock = await _context.DetailStocks
                .FirstOrDefaultAsync(s => s.DetailID == dto.DetailId);

            if (stock == null || stock.CurrentQuantity < dto.Quantity)
                return BadRequest("Недостаточно деталей на складе");

            stock.CurrentQuantity -= dto.Quantity;
            stock.ShippedQuantity += dto.Quantity;
            stock.LastUpdated = DateTime.UtcNow;

            _context.DetailTransactions.Add(new DetailTransaction
            {
                DetailID = dto.DetailId,
                Quantity = -dto.Quantity,
                TransactionType = "Shipment",
                TransactionDate = DateTime.UtcNow,
                Description = dto.Description,
                DocumentNumber = dto.DocumentNumber
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Отгрузка зарегистрирована", stock.CurrentQuantity });
        }

        [AdminWrite]
        [HttpPut("stock/{detailId}")]
        public async Task<ActionResult> AdjustStock(int detailId, [FromBody] DetailAdjustDto dto)
        {
            var stock = await _context.DetailStocks
                .FirstOrDefaultAsync(s => s.DetailID == detailId);

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

            var diff = dto.Quantity - stock.CurrentQuantity;
            stock.CurrentQuantity = dto.Quantity;
            stock.LastUpdated = DateTime.UtcNow;

            if (diff != 0)
            {
                _context.DetailTransactions.Add(new DetailTransaction
                {
                    DetailID = detailId,
                    Quantity = diff,
                    TransactionType = "Adjustment",
                    TransactionDate = DateTime.UtcNow,
                    Description = dto.Description ?? "Корректировка остатка"
                });
            }

            await _context.SaveChangesAsync();
            return Ok(stock);
        }
    }

    public class DetailMovementDto
    {
        public int DetailId { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; }
        public int? DocumentNumber { get; set; }
    }

    public class DetailAdjustDto
    {
        public int Quantity { get; set; }
        public string? Description { get; set; }
    }
}
