using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Migrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]

    public class BestSellerController : Controller
    {
        private readonly EtradeContext _context;

        public BestSellerController(IHttpContextAccessor httpContextAccessor)
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
        [HttpGet]
        public IActionResult GetBestSellers()
        {
            var bestSellers = _context.OrderItems
                .Include(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.ColorImages) // Doğru ilişki burası!
                .Include(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Color)
                .GroupBy(oi => oi.ProductVariantId)
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    ProductId = g.First().ProductVariant.Product.Id,
                    ProductName = g.First().ProductVariant.Product.Name,
                    Color = g.First().ProductVariant.Color != null ? g.First().ProductVariant.Color.Name : "Renk Yok",
                    Size = g.First().ProductVariant.Size != null ? g.First().ProductVariant.Size.Name : "Beden Yok",

                    TotalSold = g.Sum(x => x.Quantity),
                    Price = g.First().Price,
                    ImageUrl = g.First().ProductVariant.Product.ColorImages.FirstOrDefault() != null
                        ? g.First().ProductVariant.Product.ColorImages.FirstOrDefault().ImageUrl
                        : "/images/default.png"
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(20)
                .ToList();

            return Json(bestSellers);
        }
        public IActionResult Index()
        {
            return View();
        }

    }
}