using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]

    public class CategoryController : Controller
    {
        private readonly EtradeContext _context;

        public CategoryController(IHttpContextAccessor httpContextAccessor)
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
        public IActionResult index()
        {
            return View();
        }
        public IActionResult GetCategoriesWithProductCount()
        {
            var categories = _context.Categories
                .Select(c => new
                {
                    Id = c.Id,
                    Name = (string?)c.Name,  // Explicit nullable cast
                    ImageUrl = c.ImageUrl ?? "/uploads/default.jpg",
                    ProductCount = c.Products.Count
                })
                .ToList();

            var discountedProductCount = _context.Products.Count(p => p.DiscountId != null);

            if (discountedProductCount >= 0)
            {
                categories.Add(new
                {
                    Id = -1,
                    Name = (string?)"İndirimli Ürünler",  // Cast to match anonymous type
                    ImageUrl = "/uploads/tum-urunler.jpg",
                    ProductCount = discountedProductCount
                });
            }

            return Json(categories);
        }
        // Listeye Tüm Ürünler kategorisini ekle
        //var allProductsCategory = new
        // {
        //     Name = "Tüm Ürünler",
        //     ImageUrl = "/uploads/tum-urunler.jpg", // Burada nullability hatası olmaması için varsayılan değer koyduk
        //      ProductCount = totalProductCount
        //};

        // Burada listeyi uygun bir türle tekrar oluşturuyoruz
        //  var updatedCategories = new List<object> { allProductsCategory };
        // updatedCategories.AddRange(categories);





    }
}