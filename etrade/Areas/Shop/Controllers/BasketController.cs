using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using etrade.Areas.Shop.Services;


namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]

    public class BasketController : Controller
    {

        private readonly EtradeContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<BasketController> _logger;

        public BasketController(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager, ILogger<BasketController> logger)
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

            // Diğer bağımlılıkları atıyoruz
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(AuthenticationSchemes = "ShopCookie")]

        public async Task<IActionResult> Index()
        {
            var shippingFee = await _context.StoreSettings
                .Where(x => x.Id == 1)
                .Select(x => x.ShippingFee)
                .FirstOrDefaultAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Kullanıcı ID'si alınamadı. Kullanıcı giriş yapmamış olabilir.");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"Kullanıcı ID'si alındı: {userId}");

            // Aktif sepeti ve içinde pasif olan itemları alıyoruz
            var basket = await _context.Baskets
                .Include(b => b.ItemsBaskets.Where(ib => ib.IsActive == false))  // Pasif itemlar
                    .ThenInclude(ib => ib.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.ColorImages)
                .Where(b => b.UserId == userId && b.IsActive == true)
                .FirstOrDefaultAsync();

            if (basket == null)
            {
                _logger.LogWarning("Sepet bulunamadı.");
                ViewBag.ShippingFee = 0;
                ViewBag.BasketTotalPrice = 0;
                return View(new BasketViewModel());
            }

            _logger.LogInformation($"Sepet bulundu: {basket.Id}, IsActive: {basket.IsActive}");

            // Toplam tutarı hesapla
            var basketTotalPrice = basket.ItemsBaskets.Sum(item => item.TotalPrice);
            ViewBag.BasketTotalPrice = basketTotalPrice;
            ViewBag.ShippingFee = basketTotalPrice < 500 ? shippingFee : 0;

            // Sepet viewmodel oluştur
            var basketViewModel = new BasketViewModel
            {
                BasketId = basket.Id,
                ItemsBaskets = basket.ItemsBaskets.Select(item => new BasketItemViewModel
                {
                    ProductName = item.ProductVariant.Product.Name,
                    ProductPrice = item.ProductVariant.Product.Price,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    ProductImageUrl = item.ProductVariant.Product.ColorImages.FirstOrDefault()?.ImageUrl,
                    ProductId = item.ProductVariant.Product.Id,
                    VariantId = item.ProductVariant.Id,

                    ColorId = item.ProductVariant.ColorId,
                    SizeId = item.ProductVariant.SizeId,

                    SelectedColor = _context.Colors
                                            .Where(c => c.Id == item.ProductVariant.ColorId)
                                            .Select(c => c.Name)
                                            .FirstOrDefault(),

                    SelectedSize = _context.Sizes
                                           .Where(s => s.Id == item.ProductVariant.SizeId)
                                           .Select(s => s.Name)
                                           .FirstOrDefault()
                }).ToList()
            };

            return View(basketViewModel);
        }
        [HttpDelete]
        [Authorize(AuthenticationSchemes = "ShopCookie")]
        public async Task<IActionResult> RemoveItem(int variantId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Kullanıcı giriş yapmamış." });
            }

            var basket = await _context.Baskets
                .Include(b => b.ItemsBaskets)
                .FirstOrDefaultAsync(b => b.UserId == userId && b.IsActive);

            if (basket == null)
            {
                return Json(new { success = false, message = "Sepet bulunamadı." });
            }

            var itemToRemove = basket.ItemsBaskets.FirstOrDefault(ib => ib.ProductVariantId == variantId);

            if (itemToRemove != null)
            {
                _logger.LogInformation($"Silme işlemi başlatıldı, variantId: {variantId}");
                basket.ItemsBaskets.Remove(itemToRemove);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Ürün sepetten bulunamadı." });
        }
        // Sepeti onayla butonuna basıldığında Order kaydını oluşturma
        [HttpPost]
        [Authorize(AuthenticationSchemes = "ShopCookie")]
        public async Task<IActionResult> ConfirmBasket()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var basket = await _context.Baskets
                .Where(b => b.UserId == userId && b.IsActive)  // Kullanıcıya ait aktif sepeti al
                .Include(b => b.ItemsBaskets)  // Sepetteki ürünleri de yükle
                    .ThenInclude(item => item.ProductVariant)  // Ürün variantlarını yükle
                        .ThenInclude(variant => variant.Product)  // Ürün bilgilerini yükle
                .FirstOrDefaultAsync();  // İlk bulunan sepeti al

            if (basket != null && basket.ItemsBaskets != null)
            {
                var inactiveItems = basket.ItemsBaskets.Where(item => !item.IsActive).ToList();  // IsActive false olanları al

                if (inactiveItems.Any())  // Aktif olmayan ürün varsa işlem yap
                {
                    // Aktif olmayan ürünlerle işlem yapabilirsiniz
                }
                else
                {
                    return BadRequest("Sepetinizde aktif olmayan ürün bulunmuyor.");
                }
            }
            else
            {
                return BadRequest("Sepetiniz bulunmuyor.");
            }

            // Stok kontrolü yapalım
            List<string> insufficientStockItems = new List<string>(); // Stok sorunu olan ürünleri tutmak için

            foreach (var item in basket.ItemsBaskets)
            {
                if (item.ProductVariant == null)
                    return BadRequest($"Ürün varyantı bulunamadı. (ID: {item.ProductVariantId})");

                var productVariant = item.ProductVariant;

                // Stok kontrolü
                if (productVariant.Stock < item.Quantity)
                {
                    if (productVariant.Stock == 0)
                    {
                        _context.ItemsBaskets.Remove(item); // Stok tamamen bittiyse ürünü sepetten çıkar
                        insufficientStockItems.Add($"Varyant ({productVariant.Id}) stokta kalmadı ve sepetten çıkarıldı.");
                    }
                    else
                    {
                        item.Quantity = productVariant.Stock; // Stok kadar güncelle
                        insufficientStockItems.Add($"Varyant ({productVariant.Id}) stok yetersiz. Adet {productVariant.Stock} olarak güncellendi.");
                    }
                }

                // Ürün fiyatını güncelle
                item.TotalPrice = item.Quantity * item.ProductVariant.Product.Price; // Toplam fiyatı da güncelle
            }

            await _context.SaveChangesAsync();

            // Sepetteki ürünlerin toplam fiyatını hesapla
            decimal totalPrice = basket.ItemsBaskets.Sum(item => item.TotalPrice);

            // Kargo ücretini al ve ekle (500 TL altında ise)
            decimal shippingFee = 0;
            if (totalPrice < 500)
            {
                shippingFee = await _context.StoreSettings
   .Where(x => x.Id == 1)
   .Select(x => x.ShippingFee)
   .FirstOrDefaultAsync();
                totalPrice += shippingFee;
            }             // Burada artık `TotalPrice`'ı kullanıyoruz
            var orderCode = $"ORD-{Guid.NewGuid().ToString().Substring(0, 4)}{Guid.NewGuid().ToString().Substring(0, 6)}";
            // Veritabanında var mı diye kontrol et
            var existingCode = _context.Orders.FirstOrDefault(r => r.OrderCode == orderCode);
            if (existingCode != null)
            {
                // Eğer varsa, yeni bir kod oluştur
                orderCode = $"ORD-{Guid.NewGuid().ToString().Substring(0, 4)}{Guid.NewGuid().ToString().Substring(0, 6)}";
            }

            // Order kaydını oluştur
            var order = new Order
            {
                UserId = userId,
                TotalPrice = totalPrice,
                OrderStatus = "Taslak",  // Başlangıç durumu
                OrderDate = DateTime.UtcNow,
                PaymentStatus = "Ödenmedi",
                OrderCode = orderCode
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // OrderId'yi al
            int orderId = order.OrderId;
            HttpContext.Session.SetInt32("OrderId", orderId);

            // Sepetteki ürünleri OrderItem olarak kaydet
            // Sepetteki ürünleri OrderItem olarak kaydet
            foreach (var item in basket.ItemsBaskets)
            {
                // ProductVariantId'yi alarak, ilgili ProductId'yi buluyoruz
                var productVariant = _context.ProductVariants
                    .FirstOrDefault(pv => pv.Id == item.ProductVariantId);

                if (productVariant != null)
                {
                    var product = _context.Products
                        .FirstOrDefault(p => p.Id == productVariant.ProductId);

                    if (product != null)
                    {
                        // İlgili ürünün DiscountId'sini kontrol ediyoruz
                        if (product.DiscountId != null)
                        {
                            var discount = _context.Discounts
                                .FirstOrDefault(d => d.Id == product.DiscountId);

                            // Eğer indirim aktifse, OrderItem'a DiscountId ekliyoruz
                            if (discount != null && discount.IsActive)
                            {
                                var orderItem = new OrderItem
                                {
                                    OrderId = orderId,
                                    ProductVariantId = item.ProductVariantId,
                                    Quantity = item.Quantity,
                                    Price = item.ProductVariant.Product.Price, // Güncellenmiş fiyatı kullan
                                    TotalPrice = item.TotalPrice, // Güncellenmiş toplam fiyatı kullan
                                    DiscountId = discount.Id // İndirim aktifse, DiscountId ekliyoruz
                                };

                                _context.OrderItems.Add(orderItem);
                            }
                            else
                            {
                                // İndirim aktif değilse, DiscountId eklemeden kaydediyoruz
                                var orderItem = new OrderItem
                                {
                                    OrderId = orderId,
                                    ProductVariantId = item.ProductVariantId,
                                    Quantity = item.Quantity,
                                    Price = item.ProductVariant.Product.Price, // Güncellenmiş fiyatı kullan
                                    TotalPrice = item.TotalPrice // Güncellenmiş toplam fiyatı kullan
                                };

                                _context.OrderItems.Add(orderItem);
                            }
                        }
                        else
                        {
                            // Eğer ürünün DiscountId'si null ise, DiscountId eklemeden kaydediyoruz
                            var orderItem = new OrderItem
                            {
                                OrderId = orderId,
                                ProductVariantId = item.ProductVariantId,
                                Quantity = item.Quantity,
                                Price = item.ProductVariant.Product.Price, // Güncellenmiş fiyatı kullan
                                TotalPrice = item.TotalPrice // Güncellenmiş toplam fiyatı kullan
                            };

                            _context.OrderItems.Add(orderItem);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Sepet onaylandıktan sonra ödeme ekranına yönlendir
            return RedirectToAction("CreateOrder", "Order");
        }

    }
}