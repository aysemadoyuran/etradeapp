using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]


    public class FavoriteController : Controller
    {
        private readonly EtradeContext _context;

        public FavoriteController(IHttpContextAccessor httpContextAccessor)
        {
            var httpContext = httpContextAccessor.HttpContext;

            // HttpContext null kontrolü
            if (httpContext == null)
            {
                throw new InvalidOperationException("HttpContext mevcut değil. Bu, middleware'de bir sorun olduğunu gösterebilir.");
            }

            // DbContext null kontrolü
            _context = httpContext.Items["DbContext"] as EtradeContext;

            if (_context == null)
            {
                throw new Exception("DbContext bulunamadı. TenantMiddleware çalışıyor mu?");
            }
        }
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Kullanıcı ID'sini al
            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Kullanıcı giriş yapmamışsa login'e yönlendir
            }

            var favoriteProductIds = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => favoriteProductIds.Contains(p.Id) && p.IsActive) // Sadece favori ve aktif ürünleri getir
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Category)
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

            // Renkler, ColorId'ye göre gruplanacak
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
        public IActionResult GetFavoriteCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();  // Eğer kullanıcı girişi yapılmamışsa, 401 döndür
            }

            var favoriteCount = _context.Favorites.Count(f => f.UserId == userId);
            return Ok(favoriteCount);
        }

    }
}