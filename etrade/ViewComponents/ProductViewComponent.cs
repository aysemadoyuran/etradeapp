using Microsoft.AspNetCore.Mvc;
using System.Linq;
using etrade.Data.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using etrade.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace etrade.Areas.Shop.ViewComponents
{
    public class ProductViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ProductViewComponent(EtradeContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favoriteProductIds = _context.Favorites
                                             .Where(f => f.UserId == userId)
                                             .Select(f => f.ProductId)
                                             .ToList();

            ViewBag.FavoriteProductIds = favoriteProductIds;

            var products = await _context.Products
                .Where(p => p.IsActive) // Sadece aktif ürünleri getir
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    OriginalPrice = p.OriginalPrice,
                    ImageUrls = _context.ColorImages
                        .Where(img => img.ProductId == p.Id)
                        .Select(img => img.ImageUrl)
                        .ToList(),
                    Variants = p.ProductVariants.Select(v => new ProductVariantDto
                    {
                        SizeName = v.Size.Name,
                        ColorName = v.Color.Name,
                        Color = new ColorDto
                        {
                            Id = v.Color.Id,
                            Name = v.Color.Name,
                            ColorCode = v.Color.ColorCode
                        },
                        ImageUrls = _context.ColorImages
                            .Where(img => img.ProductId == p.Id && img.ColorId == v.ColorId)
                            .Select(img => img.ImageUrl)
                            .ToList()
                    }).ToList()
                })
                .ToListAsync();

            // Renkleri ColorId'ye göre gruplayarak optimize etme
            foreach (var product in products)
            {
                product.Variants = product.Variants
                    .GroupBy(v => v.Color.Id)
                    .Select(g => new ProductVariantDto
                    {
                        Color = g.First().Color,
                        ColorName = g.Key.ToString(),
                        ImageUrls = g.SelectMany(v => v.ImageUrls).Distinct().ToList(),
                        SizeName = string.Join(", ", g.Select(v => v.SizeName))
                    }).ToList();
            }

            return View(products);
        }



    }
}
