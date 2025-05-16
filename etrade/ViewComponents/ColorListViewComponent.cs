using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using etrade.Data;
using etrade.Entity;
using etrade.Data.Concrete;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class ColorListViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public ColorListViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            var colorImages = await _context.ColorImages
                .Include(ci => ci.Color) // Renk bilgisini dahil et
                .Where(ci => ci.ProductId == productId) // Ürüne ait renkleri filtrele
                .GroupBy(ci => new { ci.ColorId, ci.Color.Name }) // ColorId'ye göre grupla
                .Select(group => new
                {
                    ColorId = group.Key.ColorId,
                    ColorName = group.Key.Name,
                    ImageUrls = group.Select(ci => ci.ImageUrl).ToList() // O renk için tüm fotoğrafları listele
                })
                .ToListAsync();

            return View(colorImages);
        }
    }
}
