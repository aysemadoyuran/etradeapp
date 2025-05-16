using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using etrade.Areas.Shop.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class RefundController : Controller
    {
        private readonly EtradeContext _context;


        public RefundController(IHttpContextAccessor httpContextAccessor)
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
        public IActionResult Details()
        {
            return View();
        }
        [HttpGet]
        public async Task<ActionResult<PagedResponse<SimpleRefundDto>>> GetRefunds(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            try
            {
                var query = _context.RefundRequests
                    .OrderByDescending(r => r.RefundRequestDate)
                    .AsQueryable();

                // Durum filtresi
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.RefundStatus == status);
                }

                var totalRecords = await query.CountAsync();

                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new SimpleRefundDto
                    {
                        Id = r.RefundRequestId,
                        OrderNumber = r.Order.OrderId,
                        Status = r.RefundStatus,
                        RequestDate = r.RefundRequestDate
                    })
                    .ToListAsync();
                Console.WriteLine($"Toplam Kayıt: {totalRecords}, Dönen Öğe Sayısı: {items.Count}");


                return Ok(new PagedResponse<SimpleRefundDto>
                {
                    Data = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Bir hata oluştu: " + ex.Message });
            }
        }

        // Sayfalı yanıt için model
        public class PagedResponse<T>
        {
            public List<T> Data { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalRecords { get; set; }
            public int TotalPages { get; set; }
        }

        // DTO modeli (Mevcut yapınıza uygun)


        public class SimpleRefundDto
        {
            public int Id { get; set; }
            public int OrderNumber { get; set; }
            public string Status { get; set; }
            public DateTime RequestDate { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> GetRefundDetails(int id)
        {
            try
            {
                Console.WriteLine($"Refund detayları getiriliyor. ID: {id}");

                // 1. Önce temel refund bilgisini çekelim (IBAN bilgisini almak için)
                var refund = await _context.RefundRequests
                    .FirstOrDefaultAsync(r => r.RefundRequestId == id);

                if (refund == null)
                {
                    Console.WriteLine($"Refund bulunamadı. ID: {id}");
                    return NotFound("İade talebi bulunamadı");
                }

                // 2. Order bilgisini ayrıca çekelim
                var order = await _context.Orders
                    .Include(o => o.ShippingAddress)
                        .ThenInclude(s => s.Il)
                    .Include(o => o.ShippingAddress)
                        .ThenInclude(s => s.Ilce)
                    .Include(o => o.ShippingAddress)
                        .ThenInclude(s => s.District)
                    .Include(o => o.ShippingAddress)
                        .ThenInclude(s => s.Street)
                    .Include(o => o.PaymentMethod)
                    .FirstOrDefaultAsync(o => o.OrderId == refund.OrderId);

                Console.WriteLine($"Order bilgisi çekildi. Order ID: {order?.OrderId}");

                // 3. RefundedItems bilgisini ayrıca çekelim
                var refundedItems = await _context.RefundedItems
                    .Where(ri => ri.RefundRequestId == id)
                    .Include(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Include(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                    .Include(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                    .ToListAsync();

                Console.WriteLine($"RefundedItems çekildi. Adet: {refundedItems?.Count}");

                // 4. PaymentMethod kontrolü ve IBAN bilgisi
                string iban = null;
                string fullname = null;
                if (order?.PaymentMethod?.PaymentToken == "Kapıda Ödeme" && !string.IsNullOrEmpty(refund.Iban))
                {
                    iban = refund.Iban;
                    fullname = refund.fullName;
                    Console.WriteLine($"Kapıda Ödeme tespit edildi, IBAN bilgisi eklendi: {iban}");
                }

                // 5. Sonuç oluşturma
                var result = new
                {
                    customer = new
                    {
                        fullName = order?.ShippingAddress?.NameSurname ?? "Bilinmiyor",
                        phone = order?.ShippingAddress?.Telefon ?? "Bilinmiyor",
                        address = order?.ShippingAddress?.AcikAdres ?? "Bilinmiyor",
                        city = order?.ShippingAddress?.Il?.Ad ?? "Bilinmiyor",
                        district = order?.ShippingAddress?.Ilce?.Ad ?? "Bilinmiyor",
                        semt = order?.ShippingAddress?.District?.SemtAdi ?? "Bilinmiyor",
                        mahalle = order?.ShippingAddress?.Street?.MahalleAdi ?? "Bilinmiyor"
                    },
                    order = new
                    {
                        orderNumber = order?.OrderId ?? 0,
                        orderDate = order?.OrderDate ?? DateTime.MinValue,
                        totalAmount = order?.TotalPrice ?? 0,
                        paymentMethod = order?.PaymentMethod?.PaymentToken ?? "Bilinmiyor"
                    },
                    refund = new
                    {
                        status = refund.RefundStatus ?? "Bilinmiyor",
                        iban = iban, // Sadece Kapıda Ödeme ve IBAN varsa dolu olacak
                        fullName = fullname
                    },
                    items = refundedItems?.Select(ri => new
                    {
                        productName = ri.ProductVariant?.Product?.Name ?? "Bilinmiyor",
                        size = ri.ProductVariant?.Size?.Name ?? "Bilinmiyor",
                        color = ri.ProductVariant?.Color?.Name ?? "Bilinmiyor",
                        quantity = ri.Quantity,
                        unitPrice = ri.ProductVariant.Product.Price,
                        reasontype = ri.ReasonType
                    }) ?? Enumerable.Empty<object>()
                };

                Console.WriteLine("Refund detayları başarıyla oluşturuldu");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HATA: Refund detayları getirilirken hata oluştu. ID: {id}");
                Console.WriteLine($"Hata detayı: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, "İşlem sırasında bir hata oluştu. Lütfen sistem yöneticinize başvurun.");
            }
        }
        public IActionResult GetUserRole()
        {
            var isAdmin = User.IsInRole("admin"); // Kullanıcı Admin mi?
            return Json(new { isAdmin });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, [FromForm] string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // RefundRequest'i Order, RefundedItems ve Payment bilgileriyle birlikte getir
                var refund = await _context.RefundRequests
                    .Include(r => r.Order)
                        .ThenInclude(o => o.PaymentMethod)
                    .Include(r => r.RefundedItems)
                        .ThenInclude(ri => ri.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                    .FirstOrDefaultAsync(r => r.RefundRequestId == id);

                if (refund == null)
                {
                    return Json(new { success = false, message = "İade talebi bulunamadı" });
                }

                // Durum geçiş kontrolü
                var allowedTransitions = new Dictionary<string, string[]>
        {
            { "Beklemede", new[] { "Onaylandı", "Reddedildi" } },
            { "Onaylandı", new[] { "KargoyaVerildi" } },
            { "KargoyaVerildi", new[] { "Inceleniyor" } },
            { "Inceleniyor", new[] { "KabulEdildi", "KabulEdilmedi" } },
            { "KabulEdildi", new[] { "ParaIadesiYapildi" } },
            { "KabulEdilmedi", new[] { "UrunTeslimEdildi" } }
        };

                if (allowedTransitions.ContainsKey(refund.RefundStatus) &&
                    !allowedTransitions[refund.RefundStatus].Contains(status))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Geçersiz durum geçişi: {refund.RefundStatus} → {status}"
                    });
                }

                // Durumu güncelle
                refund.RefundStatus = status;
                refund.RefundRequestDate = DateTime.Now;
                _context.RefundRequests.Update(refund);
                if (status == "Onaylandı")
                {
                    var userCoin = await _context.UserCoins.FirstAsync(x => x.UserId == refund.Order.UserId);
                    var earnedCoin = Math.Round(refund.TotalPrice * 0.01m, 2);
                    userCoin.Coin -= earnedCoin;
                    userCoin.LastUpdated = DateTime.Now;

                    _context.UserCoins.Update(userCoin);

                    // Coin bildirimi oluştur
                    var coinNotification = new Notification
                    {
                        UserId = refund.Order.UserId,
                        Title = "Para Puan Kaybettiniz!",
                        Message = $"#{refund.RefundCode} numaralı iadenizden {earnedCoin} para puan kaybettiniz.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        IsGlobal = false,
                        NotificationType = "Coin"
                    };

                    _context.Notifications.Add(coinNotification);
                    await _context.SaveChangesAsync();
                }

                // Eğer durum "ParaIadesiYapildi" ise
                if (status == "ParaIadesiYapildi")
                {
                    // 1. Sipariş toplam fiyatını güncelle
                    if (refund.Order != null)
                    {
                        refund.Order.TotalPrice -= refund.TotalPrice;
                        _context.Orders.Update(refund.Order);

                        // Siparişin ödeme durumunu güncelleyebilirsiniz (opsiyonel)
                        // refund.Order.PaymentStatus = "PartiallyRefunded";
                    }

                    // 2. Ödeme iadesi yap (Kapıda Ödeme değilse)
                    if (refund.Order?.PaymentMethod?.PaymentToken != "Kapıda Ödeme")
                    {
                        try
                        {
                            var paymentId = refund.Order.PaymentMethod.PaymentToken;
                            var amount = refund.TotalPrice;

                            // Iyzico iade işlemi
                            var refundResult = await RefundPaymentByAmountAsync(paymentId, amount);

                            if (refundResult == null || refundResult.Status?.ToLower() != "success")
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    success = false,
                                    message = $"Iyzico iade işlemi başarısız: {refundResult?.ErrorMessage}"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            return Json(new
                            {
                                success = false,
                                message = $"Iyzico iade işlemi sırasında hata: {ex.Message}"
                            });
                        }
                    }

                    // 3. Stok güncellemesi ve stok hareket kaydı
                    foreach (var item in refund.RefundedItems)
                    {
                        if (item.ProductVariant != null)
                        {
                            // Stok miktarını artır
                            item.ProductVariant.Stock += item.Quantity;
                            _context.ProductVariants.Update(item.ProductVariant);

                            // Stok hareket kaydı oluştur
                            var stockMovement = new StockMovement
                            {
                                ProductVariantId = item.ProductVariantId,
                                MovementType = "İade",
                                Quantity = item.Quantity,
                                Date = DateTime.Now,
                                Description = $"İade işlemi - Sipariş No: {refund.OrderId}",
                                CurrentStock = item.ProductVariant.Stock
                            };
                            await _context.StockMovements.AddAsync(stockMovement);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<Refund> RefundPaymentByAmountAsync(string paymentId, decimal amount)
        {
            if (string.IsNullOrEmpty(paymentId))
            {
                throw new ArgumentNullException(nameof(paymentId), "Payment ID cannot be null or empty.");
            }
            Options options = new Options
            {
                ApiKey = "sandbox-F6PR5Gda82x2lahdseMEcnpjAyDqUQSP",
                SecretKey = "sandbox-ctcF7lH8ygz2sUn7kEhjTiCstD0U9XvA",
                BaseUrl = "https://sandbox-api.iyzipay.com"
            };

            var refundRequest = new CreateAmountBasedRefundRequest
            {
                PaymentId = paymentId,
                Price = amount.ToString("0.00", CultureInfo.InvariantCulture),
                Ip = "127.0.0.1",
                Locale = Locale.TR.ToString(),
                ConversationId = "251" // Rastgele bir ID
            };

            try
            {
                // Ödeme ID bazlı iade işlemi yapılıyor
                return await Refund.CreateAmountBasedRefundRequest(refundRequest, options);
            }
            catch (Exception ex)
            {
                // API hatası loglanıyor
                Console.WriteLine($"İyzico ödeme ID bazlı iade işlemi sırasında hata: {ex.Message}");
                throw new Exception("İyzico ödeme ID bazlı iade işlemi sırasında hata oluştu: " + ex.Message);
            }
        }



    }

}
