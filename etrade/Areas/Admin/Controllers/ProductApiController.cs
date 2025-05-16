using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]


    [Route("admin/api/[controller]")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly EtradeContext _context;

        public ProductApiController(IHttpContextAccessor httpContextAccessor)
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

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Ürün bulunamadı" });
            }

            var productDto = new
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                SubCategoryId = product.SubCategoryId,
                SubCategoryName = product.SubCategory?.Name,
                Variants = product.ProductVariants?
                    .GroupBy(v => new { v.ColorId, v.Color?.Name })
                    .Select(g => new
                    {
                        ColorId = g.Key.ColorId,
                        ColorName = g.Key.Name,
                        Sizes = g.Select(v => new
                        {
                            SizeId = v.SizeId,
                            SizeName = v.Size?.Name,
                            Stock = v.Stock
                        }).ToList(),
                        ImageUrls = _context.ColorImages
                            .Where(img => img.ColorId == g.Key.ColorId && img.ProductId == product.Id)
                            .Select(img => img.ImageUrl)
                            .ToList()
                    }).ToList()
            };

            return Ok(productDto);
        }


    }
}
