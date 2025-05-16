using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class StockListViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public StockListViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .GroupBy(v => v.Color.Name) // Renge göre grupla
                .Select(g => new
                {
                    ColorName = g.Key, // Grup anahtarı (renk adı)
                    Sizes = g
                        .OrderBy(v => v.SizeId) // SizeId'ye göre sıralama (azdan çoğa)
                        .Select(v => new { SizeName = v.Size.Name, v.Stock, v.SizeId })
                        .ToList(),
                    TotalStock = g.Sum(v => v.Stock) // Toplam stok hesaplama
                })
                .ToListAsync();

            // Modelleri Razor view'ına geçiriyoruz
            return View(variants);
        }



    }
}