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
    [Route("api/[controller]")]
    [ApiController]
    public class BasketApiController : ControllerBase
    {
        private readonly EtradeContext _context;

        public BasketApiController(EtradeContext context)
        {
            _context = context;
        }

        [HttpPost("addtobasket")]
        public async Task<IActionResult> AddToBasket(int basketId, int productId, int colorId, int sizeId, int quantity)
        {
            // Ürün bilgilerini alıyoruz
            var product = await _context.Products
                                         .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return BadRequest($"Ürün bulunamadı. Ürün ID: {productId}");
            }

            // Ürün varyantını alıyoruz (Renk ve beden)
            var productVariant = await _context.ProductVariants
                                               .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.ColorId == colorId && pv.SizeId == sizeId);

            if (productVariant == null)
            {
                return BadRequest($"Ürün varyantı bulunamadı. Ürün ID: {productId}, Renk ID: {colorId}, Beden ID: {sizeId}");
            }

            // Ürünün fiyatını Product modelinden alıyoruz
            decimal price = product.Price;

            // BasketItem kaydını oluşturuyoruz
            var basketItem = new ItemsBasket
            {
                BasketId = basketId,
                ProductVariantId = productVariant.Id,
                Quantity = quantity,
                TotalPrice = price * quantity // Fiyat hesabı: Product modelinden fiyat alınarak hesaplanır
            };

            _context.ItemsBaskets.Add(basketItem);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

}