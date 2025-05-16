using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Shop.Services;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class DashboardController : Controller
    {
        private readonly EtradeContext _context;


        public DashboardController(IHttpContextAccessor httpContextAccessor)
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
            var currentFee = _context.StoreSettings
                .Where(x => x.Id == 1)
                .Select(x => x.ShippingFee)
                .FirstOrDefault();
            Console.WriteLine($"Mevcut Kargo Ücreti: {currentFee}");
            return View(new ShippingFeeViewModel { ShippingFee = currentFee });
        }
        [HttpPost]
        public async Task<IActionResult> Edit(ShippingFeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    isSuccess = false,
                    message = "Geçersiz veri gönderildi.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            try
            {
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync(x => x.Id == 1);

                if (storeSetting == null)
                {
                    return NotFound(new
                    {
                        isSuccess = false,
                        message = "Ayar bulunamadı."
                    });
                }

                storeSetting.ShippingFee = model.ShippingFee;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    isSuccess = true,
                    message = "Kargo ücreti başarıyla güncellendi.",
                    updatedFee = model.ShippingFee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isSuccess = false,
                    message = "Bir hata oluştu: " + ex.Message
                });
            }
        }
        public async Task<IActionResult> GetDeliveredOrderCount()
        {
            var deliveredCount = await _context.Orders
                .Where(o => o.OrderStatus == "Teslim Edildi")
                .CountAsync();

            var totalCount = await _context.Orders.CountAsync();

            return Ok(new
            {
                title = "Teslim Edilen Sipariş Sayısı",
                key = "deliveredOrderCount",
                delivered = deliveredCount,
                total = totalCount
            });
        }
        public async Task<IActionResult> GetTodayTopStockMovements()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var stockMovements = await _context.StockMovements
                .Include(sm => sm.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Where(sm => sm.Date >= today && sm.Date < tomorrow)
                .OrderByDescending(sm => sm.Date)
                .Take(5)
                .Select(sm => new
                {
                    sm.Id,
                    ProductName = sm.ProductVariant.Product.Name,
                    sm.Quantity,
                    sm.MovementType,
                    CreatedDate = sm.Date.ToLocalTime().ToString("HH:mm:ss")
                })
                .ToListAsync();

            return Ok(stockMovements);
        }
        public async Task<IActionResult> GetCampaignStats()
        {
            var now = DateTime.Now;

            var completed = await _context.Discounts
                .Where(d => d.EndDateTime < now)
                .CountAsync();

            var upcoming = await _context.Discounts
                .Where(d => d.StartDateTime > now)
                .CountAsync();

            var active = await _context.Discounts
                .Where(d => d.IsActive)
                .CountAsync();

            var upcomingCampaigns = await _context.Discounts
                .Where(d => d.StartDateTime > now)
                .OrderBy(d => d.StartDateTime)
                .Take(5)
                .Select(d => new
                {
                    d.Name,
                    StartDate = d.StartDateTime.ToString("dd.MM.yyyy HH:mm"),
                    EndDate = d.EndDateTime.ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            return Ok(new
            {
                title = "Kampanya Durumları",
                key = "campaignStats",
                completed,
                upcoming,
                active,
                upcomingCampaigns
            });
        }
        public async Task<IActionResult> GetBestSellingProduct()
        {
            var mostSoldVariant = await _context.OrderItems
                .GroupBy(oi => oi.ProductVariantId)
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(g => g.TotalSold)
                .FirstOrDefaultAsync();

            if (mostSoldVariant == null)
            {
                return Ok(new
                {
                    title = "En Çok Satılan Ürün",
                    key = "bestSellingProduct",
                    productId = 0,
                    productName = "",
                    totalSold = 0,
                    productCode = ""
                });
            }

            var productId = await _context.ProductVariants
                .Where(pv => pv.Id == mostSoldVariant.ProductVariantId)
                .Select(pv => pv.ProductId)
                .FirstOrDefaultAsync();

            var productName = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
            var productCode = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => p.ProductCode)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                title = "En Çok Satılan Ürün",
                key = "bestSellingProduct",
                productId = productId,
                productName = productName,
                totalSold = mostSoldVariant.TotalSold,
                productCode = productCode
            });
        }
        public async Task<IActionResult> GetMostSoldCity()
        {
            var mostSoldCity = await _context.Orders
                .Include(o => o.ShippingAddress)  // İlişkili Address'leri dahil ediyoruz
                .ThenInclude(a => a.Il)  // Address içindeki City'yi dahil ediyoruz
                .GroupBy(o => o.ShippingAddress.Il.Ad)  // Şehir adına göre grupla
                .Select(group => new
                {
                    City = group.Key,
                    SalesCount = group.Count()
                })
                .OrderByDescending(x => x.SalesCount)
                .FirstOrDefaultAsync();

            if (mostSoldCity == null)
            {
                return NotFound(new { message = "No sales data found." });
            }

            return Ok(mostSoldCity);
        }
        public IActionResult GetTotalSalesAndRevenue()
        {
            var salesQuery = _context.OrderItems.AsQueryable();

            var totalSales = salesQuery
                .GroupBy(oi => 1) // Tüm öğeleri grupluyoruz, çünkü toplam bir değer istiyoruz
                .Select(g => new
                {
                    TotalSoldQuantity = g.Sum(oi => oi.Quantity), // Toplam satılan ürün adedi
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price) // Toplam gelir (adedi x fiyatı)
                }).FirstOrDefault();

            return Json(totalSales);
        }
        public async Task<IActionResult> GetTotalRefundedItemsAndAmount()
        {
            // Para iadesi yapılmış refund request'lerini alıyoruz
            var refundedRequests = await _context.RefundRequests
                .Where(rr => rr.RefundStatus == "ParaIadesiYapildi")  // "ParaIadesiYapildi" olanları al
                .Select(rr => rr.RefundRequestId)  // Para iadesi yapılmış refund request'lerin ID'lerini al
                .ToListAsync();

            if (refundedRequests.Count == 0)
            {
                return NotFound(new { message = "No refunded items found." });
            }

            // Bu refund request'lere bağlı RefundedItems'leri alıp, toplam iade edilen ürün miktarını hesaplıyoruz
            var refundedItems = await _context.RefundedItems
                .Where(ri => refundedRequests.Contains(ri.RefundRequestId))  // Para iadesi yapılmış refund request'lere bağlı ürünler
                .ToListAsync();

            // Toplam iade edilen ürün miktarını hesaplıyoruz
            var totalQuantityRefunded = refundedItems.Sum(ri => ri.Quantity);

            // Para iadesi yapılmış refund request'lerinin toplam fiyatını hesaplıyoruz
            var totalRefundedAmount = await _context.RefundRequests
                .Where(rr => refundedRequests.Contains(rr.RefundRequestId))  // Para iadesi yapılmış refund request'leri
                .SumAsync(rr => rr.TotalPrice);  // RefundRequest'teki TotalPrice alanını kullanıyoruz

            return Ok(new
            {
                TotalRefundedItemsCount = totalQuantityRefunded,
                TotalRefundedAmount = totalRefundedAmount
            });
        }
        public async Task<IActionResult> GetCustomerUserCount()
        {
            // "customer" rolüne sahip kullanıcıların sayısını alıyoruz
            var customerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "customer");

            if (customerRole == null)
            {
                return NotFound(new { message = "Customer role not found." });
            }

            var customerUserCount = await _context.UserRoles
                .Where(ur => ur.RoleId == customerRole.Id)  // "customer" rolüne sahip kullanıcıları filtreliyoruz
                .CountAsync();

            return Ok(new { CustomerUserCount = customerUserCount });
        }
        public async Task<IActionResult> GetFrequentCustomersCount()
        {
            // 5'ten fazla sipariş veren kullanıcıların sayısını almak için Order tablosunda gruplama yapıyoruz
            var frequentCustomers = await _context.Orders
                .GroupBy(o => o.UserId)  // UserId'ye göre grupla
                .Where(g => g.Count() > 5)  // 5'ten fazla siparişi olanları filtrele
                .Select(g => g.Key)  // Grupladığımız UserId'yi seçiyoruz
                .ToListAsync();

            // Sonuç
            return Ok(new { FrequentCustomersCount = frequentCustomers.Count });
        }
        public async Task<IActionResult> GetUsersWithNoOrders()
        {
            // "customer" rolündeki kullanıcıları buluyoruz
            var customerRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "customer");

            if (customerRole == null)
            {
                return NotFound(new { message = "Customer role not found." });
            }

            // "customer" rolündeki kullanıcıların ID'lerini alıyoruz
            var customerUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == customerRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Sipariş vermemiş kullanıcıları buluyoruz
            var usersWithNoOrders = await _context.Users
                .Where(u => customerUserIds.Contains(u.Id) && !u.Orders.Any())  // Kullanıcı sipariş vermemişse
                .ToListAsync();

            // Sonuç: Sipariş vermeyen kullanıcıların sayısını döndürüyoruz
            return Ok(new { UsersWithNoOrdersCount = usersWithNoOrders.Count });
        }
        public async Task<IActionResult> GetPaymentMethodPercentage()
        {
            // Tüm siparişlerin toplam sayısını alıyoruz
            var totalOrders = await _context.Orders.CountAsync();

            if (totalOrders == 0)
            {
                return Ok(new { OnlinePaymentPercentage = 0, CashOnDeliveryPercentage = 0 });
            }

            // İyzico aracılığıyla ödeme yapılan siparişlerin sayısını alıyoruz (Ödendi)
            var onlinePaymentsCount = await _context.Orders
                .Where(o => o.PaymentStatus == "Ödendi") // Ödendi olan siparişler
                .CountAsync();

            // Kapıda ödeme yapılan siparişlerin sayısını alıyoruz
            var cashOnDeliveryCount = await _context.Orders
                .Where(o => o.PaymentStatus == "Kapıda Ödeme") // Kapıda ödeme olan siparişler
                .CountAsync();

            // Yüzdesel oranları hesaplıyoruz
            var onlinePaymentPercentage = (double)onlinePaymentsCount / totalOrders * 100;
            var cashOnDeliveryPercentage = (double)cashOnDeliveryCount / totalOrders * 100;

            // Sonuçları JSON formatında dönüyoruz
            return Ok(new
            {
                OnlinePaymentPercentage = Math.Round(onlinePaymentPercentage, 2),
                CashOnDeliveryPercentage = Math.Round(cashOnDeliveryPercentage, 2)
            });
        }
        public async Task<IActionResult> GetUsersWithCommentsCount()
        {
            // Yorum yapan kullanıcıları ve yorum sayısını gruplama
            var usersWithCommentsCount = await _context.Comments
                .GroupBy(c => c.UserId) // Kullanıcı ID'sine göre gruplama
                .Select(group => new
                {
                    UserId = group.Key, // Kullanıcı ID'si
                    CommentsCount = group.Count() // Yorum sayısı
                })
                .ToListAsync();

            // Yorum yapan kullanıcıların sayısını döndürme
            var totalUsersWithComments = usersWithCommentsCount.Count;

            return Ok(new { TotalUsersWithComments = totalUsersWithComments });
        }
        public async Task<IActionResult> GetUsersUsedCampaignsCount()
        {
            // Kampanya kullanılan order item'lar
            var userIds = await _context.OrderItems
                .Where(oi => oi.DiscountId != null) // DiscountId null olmayanlar = kampanyalı ürünler
                .Include(oi => oi.Order) // Order ilişkisini dahil et
                .Select(oi => oi.Order.UserId) // Order üzerinden UserId al
                .Distinct() // Aynı kullanıcıyı sadece bir kez say
                .ToListAsync();

            // Farklı kullanıcı sayısını dön
            var userCount = userIds.Count;

            return Ok(new { CampaignUserCount = userCount });
        }
        [HttpGet]
        public async Task<IActionResult> GetUsersRegisteredWithInvitationCode()
        {
            var invitedUserCount = await _context.Users
                .Where(u => u.InvitationCode != null)
                .CountAsync();

            return Ok(new { InvitedUserCount = invitedUserCount });
        }
        public async Task<IActionResult> GetTotalConvertedCoinsAndCount()
        {
            var coinConversionCoupons = await _context.Coupons
                .Where(c => c.CouponCategory == "CoinConversion")
                .ToListAsync();

            var totalConvertedCoins = coinConversionCoupons.Sum(c => c.DiscountValue);
            var totalConversionCount = coinConversionCoupons.Count;

            return Ok(new
            {
                TotalConvertedCoins = totalConvertedCoins,
                TotalConversionCount = totalConversionCount
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetTopSalesCities()
        {
            var cityData = await _context.Orders
                .Where(o => o.OrderStatus == "Teslim Edildi")
                .Include(o => o.ShippingAddress)
                    .ThenInclude(a => a.Il)
                .GroupBy(o => o.ShippingAddress.Il)  // İl bazında gruplama
                .Select(group => new
                {
                    PlateCode = group.Key.Id.ToString(),  // Plaka kodunu string'e çeviriyoruz
                    CityName = group.Key.Ad,
                    SalesCount = group.Count()  // Şehirdeki sipariş sayısını alıyoruz
                })
                .OrderByDescending(city => city.SalesCount)  // Sipariş sayısına göre azalan sırayla sıralıyoruz
                .Take(10)  // Sadece ilk 10 şehri alıyoruz
                .ToListAsync();

            return Ok(cityData);
        }
    }
}