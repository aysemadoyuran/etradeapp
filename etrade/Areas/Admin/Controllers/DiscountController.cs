using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Enums;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class DiscountController : Controller
    {
        private readonly EtradeContext _context;

        public DiscountController(IHttpContextAccessor httpContextAccessor)
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

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult GetUserRole()
        {
            var isAdmin = User.IsInRole("admin"); // Kullanıcı Admin mi?
            return Json(new { isAdmin });
        }
        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = _context.Products
                .Select(p => new
                {
                    p.Id,
                    p.Name
                })
                .ToList();

            return Ok(products);
        }
        [HttpGet]
        public IActionResult GetCategories()
        {
            var categories = _context.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name
                })
                .ToList();

            return Ok(categories);
        }
        [HttpPost]
        public IActionResult Create([FromBody] DiscountViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Geçersiz veri." });
            }

            if (!Enum.TryParse(model.DiscountType, out DiscountType discountType))
            {
                return BadRequest(new { success = false, message = $"Geçersiz indirim türü: {model.DiscountType}" });
            }

            if (!Enum.TryParse(model.ConditionType, out ConditionType conditionType))
            {
                return BadRequest(new { success = false, message = $"Geçersiz koşul türü: {model.ConditionType}" });
            }

            if (model.SelectedIds == null || !model.SelectedIds.Any())
            {
                return BadRequest(new { success = false, message = "Lütfen en az bir ürün veya kategori seçin." });
            }

            // Tarih kontrolü
            if (model.StartDateTime.Date < DateTime.Now.Date)
            {
                return BadRequest(new { success = false, message = "Başlangıç tarihi geçmişte olamaz." });
            }

            if (model.EndDateTime < model.StartDateTime)
            {
                return BadRequest(new { success = false, message = "Bitiş tarihi, başlangıç tarihinden önce olamaz." });
            }

            try
            {
                var discount = new Discount
                {
                    Name = model.DiscountName,
                    DiscountType = discountType,
                    Value = model.Value,
                    ConditionType = conditionType,
                    StartDateTime = model.StartDateTime,
                    EndDateTime = model.EndDateTime,
                    IsActive = model.IsActive
                };

                _context.Discounts.Add(discount);
                _context.SaveChanges(); // Discount kaydedildi, id alındı

                // Ürün bazlı indirim
                if (conditionType == ConditionType.Product)
                {
                    var products = _context.Products
                        .Where(p => model.SelectedIds.Contains(p.Id))
                        .ToList();

                    foreach (var product in products)
                    {
                        var discountProduct = new DiscountProduct
                        {
                            DiscountId = discount.Id,
                            ProductId = product.Id
                        };
                        _context.DiscountProducts.Add(discountProduct);
                        _context.Products.Update(product);
                    }
                }
                // Kategori bazlı indirim
                else if (conditionType == ConditionType.Category)
                {
                    var existingCategories = _context.DiscountCategories
                        .Where(dc => dc.DiscountId == discount.Id && model.SelectedIds.Contains(dc.CategoryId))
                        .ToList();

                    foreach (var categoryId in model.SelectedIds)
                    {
                        // Mevcut kategorilerle çakışma olmasın diye kontrol yapıyoruz
                        if (!existingCategories.Any(ec => ec.CategoryId == categoryId))
                        {
                            var discountCategory = new DiscountCategory
                            {
                                DiscountId = discount.Id,
                                CategoryId = categoryId
                            };
                            _context.DiscountCategories.Add(discountCategory);
                        }
                    }
                }

                _context.SaveChanges(); // DiscountProducts ve DiscountCategories kaydedildi

                return Ok(new { success = true, message = "İndirim başarıyla oluşturuldu." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult Update([FromBody] DiscountViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Geçersiz veri." });
            }

            if (!Enum.TryParse(model.DiscountType, out DiscountType discountType))
            {
                return BadRequest(new { success = false, message = $"Geçersiz indirim türü: {model.DiscountType}" });
            }

            if (!Enum.TryParse(model.ConditionType, out ConditionType conditionType))
            {
                return BadRequest(new { success = false, message = $"Geçersiz koşul türü: {model.ConditionType}" });
            }

            if (model.SelectedIds == null || !model.SelectedIds.Any())
            {
                return BadRequest(new { success = false, message = "Lütfen en az bir ürün veya kategori seçin." });
            }

            // Bitiş tarihi başlangıç tarihinden önce olamaz
            if (model.StartDateTime >= model.EndDateTime)
            {
                return BadRequest(new { success = false, message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });
            }

            try
            {
                // Mevcut indirim kaydını veritabanından al
                var discount = _context.Discounts.FirstOrDefault(d => d.Id == model.DiscountId);
                if (discount == null)
                {
                    return BadRequest(new { success = false, message = "İndirim bulunamadı." });
                }

                // Eğer indirim aktifse, düzenleme yapılmasına izin verme
                if (discount.IsActive)
                {
                    return BadRequest("Aktif kampanyaların düzenlenmesine izin verilmemektedir. Sadece silebilirsiniz.");
                }

                // İndirim verilerini güncelle
                discount.Name = model.DiscountName;
                discount.DiscountType = discountType;
                discount.Value = model.Value;
                discount.ConditionType = conditionType;
                discount.StartDateTime = model.StartDateTime;
                discount.EndDateTime = model.EndDateTime;
                discount.IsActive = model.IsActive;

                _context.Discounts.Update(discount);

                // Eski ilişkileri sil
                _context.DiscountProducts.RemoveRange(_context.DiscountProducts.Where(dp => dp.DiscountId == discount.Id));
                _context.DiscountCategories.RemoveRange(_context.DiscountCategories.Where(dc => dc.DiscountId == discount.Id));

                // Ürün bazında işlem yap
                if (conditionType == ConditionType.Product)
                {
                    foreach (var productId in model.SelectedIds)
                    {
                        var discountProduct = new DiscountProduct
                        {
                            DiscountId = discount.Id,
                            ProductId = productId
                        };
                        _context.DiscountProducts.Add(discountProduct);
                    }
                }
                // Kategori bazında işlem yap
                else if (conditionType == ConditionType.Category)
                {
                    foreach (var categoryId in model.SelectedIds)
                    {
                        var discountCategory = new DiscountCategory
                        {
                            DiscountId = discount.Id,
                            CategoryId = categoryId
                        };
                        _context.DiscountCategories.Add(discountCategory);
                    }
                }

                _context.SaveChanges();
                return Ok(new { success = true, message = "İndirim başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
            }
        }
        public IActionResult Calendar()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetDiscountsForCalendar()
        {
            var discounts = _context.Discounts
                .Where(d => d.StartDateTime >= DateTime.UtcNow.AddMonths(-1)) // Son 1 ayı göstermek için
                .Select(d => new
                {
                    id = d.Id,
                    title = d.Name,  // Başlık
                    start = d.StartDateTime.ToString("yyyy-MM-dd HH:mm"), // Başlangıç tarihi ve saati
                    end = d.EndDateTime.ToString("yyyy-MM-dd HH:mm"),     // Bitiş tarihi ve saati
                    description = GetDiscountStatus(d),  // Kampanya durumu açıklaması
                    backgroundColor = GetDiscountColor(d)  // Kampanya durumu için renk
                })
                .ToList();

            return Ok(discounts);
        }

        private static string GetDiscountStatus(Discount discount)
        {
            var now = DateTime.UtcNow;  // Şu anki UTC saati

            // Veritabanındaki tarihlerin UTC'ye göre olduğunu varsayıyorum, bu yüzden UTC'yi kontrol ediyoruz.
            var startDateTime = discount.StartDateTime.ToUniversalTime();  // Kampanya başlangıç zamanı
            var endDateTime = discount.EndDateTime.ToUniversalTime();      // Kampanya bitiş zamanı

            if (now < startDateTime)
            {
                return "Henüz Başlamadı";  // Başlangıç tarihi gelmediyse
            }
            else if (now >= startDateTime && now <= endDateTime)
            {
                return "Aktif Kampanya";  // Şu an aktifse
            }
            else
            {
                return "Tamamlanan Kampanya";  // Bitiş tarihi geçmişse
            }
        }

        private static string GetDiscountColor(Discount discount)
        {
            var now = DateTime.UtcNow;
            var startDateTime = discount.StartDateTime.ToUniversalTime();  // Kampanya başlangıç zamanı
            var endDateTime = discount.EndDateTime.ToUniversalTime();      // Kampanya bitiş zamanı

            if (now < discount.StartDateTime)
            {
                return "#ffc107";  // Sarı renk - Henüz Başlamayan
            }
            else if (now >= discount.StartDateTime && now <= discount.EndDateTime)
            {
                return "#28a745";  // Yeşil renk - Aktif Kampanya
            }
            else
            {
                return "#dc3545";  // Kırmızı renk - Tamamlanan
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDiscounts()
        {
            var discounts = await _context.Discounts
                .OrderByDescending(d => d.Id) // ID'ye göre büyükten küçüğe sırala
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.DiscountType,
                    d.Value,
                    d.ConditionType,
                    d.ConditionValue,
                    d.StartDateTime,
                    d.EndDateTime,
                    d.BuyQuantity,
                    d.FreeQuantity,
                    d.IsActive
                })
                .ToListAsync();

            return Ok(discounts);
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            // İndirim bilgisini bul
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null)
            {
                return NotFound("İndirim bulunamadı.");
            }

            // Yalnızca seçilen indirimle ilişkilendirilmiş ürünleri bul
            var productsWithDiscount = await _context.Products
                .Where(p => p.DiscountId == id)
                .ToListAsync();

            foreach (var product in productsWithDiscount)
            {
                // Orijinal fiyatı Price'a ata ve ardından OriginalPrice'ı null yap
                if (product.OriginalPrice.HasValue)
                {
                    product.Price = product.OriginalPrice.Value; // Orijinal fiyatı Price'a ata
                }
                product.OriginalPrice = 0.00m; // Orijinal fiyatı null yap
                product.DiscountedPrice = null; // DiscountedPrice'ı null yap
                product.DiscountId = null; // İlgili DiscountId'yi null yap
            }

            // Değişiklikleri kaydet
            await _context.SaveChangesAsync();

            // Seçilen indirimi sil
            _context.Discounts.Remove(discount);
            await _context.SaveChangesAsync();

            return Ok("İndirim ve ilgili hesaplamalar başarıyla silindi.");
        }
        public IActionResult List()
        {
            return View();
        }
        public IActionResult Edit()
        {
            return View();
        }
        public IActionResult DiscountProduct()
        {
            return View();
        }
        public async Task<IActionResult> GetDiscountById(int id)
        {
            var discount = await _context.Discounts
                .Include(d => d.DiscountProducts)
                    .ThenInclude(dp => dp.Product)
                .Include(d => d.DiscountCategories)
                    .ThenInclude(dc => dc.Category)
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.DiscountType,
                    d.Value,
                    d.ConditionType,
                    d.ConditionValue,
                    d.StartDateTime,
                    d.EndDateTime,
                    d.BuyQuantity,
                    d.FreeQuantity,
                    d.IsActive,
                    DiscountProducts = d.DiscountProducts.Select(dp => new
                    {
                        dp.ProductId,
                        ProductName = dp.Product.Name
                    }).ToList(),
                    DiscountCategories = d.DiscountCategories.Select(dc => new
                    {
                        dc.CategoryId,
                        CategoryName = dc.Category.Name
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (discount == null)
            {
                return NotFound(new { success = false, message = "İndirim bulunamadı." });
            }

            return Ok(new { success = true, discount });
        }
        public IActionResult GetProductDiscounts()
        {
            var currentDate = DateTime.Now;

            // Ürün bazlı indirimleri al
            var productsWithDiscount = _context.Products
                .Where(p => p.DiscountId != null)
                .Select(p => new
                {
                    Product = p,
                    Discount = _context.Discounts.FirstOrDefault(d => d.Id == p.DiscountId)
                })
                .ToList();

            // Kategori bazlı indirimleri al
            var categoryDiscounts = _context.DiscountCategories
                .Include(dc => dc.Discount)
                .Where(dc => dc.Discount.EndDateTime > currentDate)
                .ToList();

            var productDiscountInfos = new List<object>();

            // Ürün bazlı indirimleri ekle
            foreach (var item in productsWithDiscount)
            {
                var product = item.Product;
                var discount = item.Discount;

                if (discount != null && discount.EndDateTime > currentDate)
                {
                    var remainingTime = discount.EndDateTime - currentDate;
                    if (remainingTime.TotalSeconds > 0)
                    {
                        productDiscountInfos.Add(new
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            DiscountName = discount.Name,
                            DiscountRemainingTime = $"{remainingTime.Days} gün {remainingTime.Hours} saat {remainingTime.Minutes} dakika kaldı"
                        });
                    }
                }
            }

            // Kategori bazlı indirimleri kontrol et
            foreach (var categoryDiscount in categoryDiscounts)
            {
                var categoryId = categoryDiscount.CategoryId;
                var discount = categoryDiscount.Discount;

                var productsInCategory = _context.Products
                    .Where(p => p.CategoryId == categoryId && p.DiscountId == null) // Ürün bazlı indirimi olmayanları al
                    .ToList();

                foreach (var product in productsInCategory)
                {
                    var remainingTime = discount.EndDateTime - currentDate;
                    if (remainingTime.TotalSeconds > 0)
                    {
                        productDiscountInfos.Add(new
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            DiscountName = discount.Name,
                            DiscountRemainingTime = $"{remainingTime.Days} gün {remainingTime.Hours} saat {remainingTime.Minutes} dakika kaldı"
                        });
                    }
                }
            }

            return Ok(productDiscountInfos);
        }

    }
}