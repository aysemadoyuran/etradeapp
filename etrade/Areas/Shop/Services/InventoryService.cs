using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Services
{
    public class InventoryService
    {
        private readonly EtradeContext _context;

        public InventoryService(EtradeContext context)
        {
            _context = context;
        }

        public async Task UpdateStockAsync(int productVariantId, int quantityChanged, string description, string movementType, int currentStock)
        {
            var productVariant = await _context.ProductVariants
                                                .Include(pv => pv.StockMovements)
                                                .FirstOrDefaultAsync(pv => pv.Id == productVariantId);

            if (productVariant == null)
            {
                throw new Exception("Product variant not found.");
            }

            // Stok hareketini kaydedelim
            var stockMovement = new StockMovement
            {
                ProductVariantId = productVariantId,
                Quantity = quantityChanged,
                Date = DateTime.UtcNow,
                MovementType = movementType,
                Description = description,
                CurrentStock = productVariant.Stock // Burada CurrentStock, zaten güncel stoku yansıtır.
            };

            // Stok güncelleniyor
            productVariant.Stock += quantityChanged;

            _context.StockMovements.Add(stockMovement);
            await _context.SaveChangesAsync();
        }
    }
}