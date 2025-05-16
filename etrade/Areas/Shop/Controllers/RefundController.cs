using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using etrade.Areas.Shop.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static etrade.Areas.Shop.Services.RefundService;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Authorize(Roles = "customer")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]

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
        public IActionResult Index(string OrderCode)
        {
            // Sipariş bilgilerini veritabanından al
            var order = _context.Orders
                .Include(o => o.PaymentMethod) // Payment bilgilerini include et
                .FirstOrDefault(o => o.OrderCode == OrderCode);

            if (order == null)
            {
                return NotFound();
            }

            // ViewData ile gerekli bilgileri gönder
            ViewData["OrderId"] = order.OrderId; // OrderId'yi gönderiyoruz
            ViewData["PaymentToken"] = order.PaymentMethod?.PaymentToken ?? "Bilinmiyor";

            return View();
        }
        public IActionResult GetOrderDetails(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Include(o => o.PaymentMethod)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Siparişe ait ürünlerin ve iade taleplerinin listesini al
            var refundItemProductVariants = _context.RefundedItems
                .Include(ri => ri.RefundRequest) // RefundRequest ilişkisini dahil et
                .Where(ri => ri.RefundRequest.OrderId == id)
                .GroupBy(ri => ri.ProductVariantId) // Her ProductVariantId'ye göre gruplama yapıyoruz
                .Select(group => new
                {
                    ProductVariantId = group.Key,
                    TotalRefunded = group.Sum(g => g.Quantity) // Her ürün için toplam iade adedini hesaplıyoruz
                })
                .ToList();

            // Siparişteki ürünleri ve iade talebine göre filtreleme işlemi
            var response = new
            {
                orderId = order.OrderId,
                paymentToken = order.PaymentMethod.PaymentToken,
                orderItems = order.OrderItems
                    .Where(oi =>
                        !refundItemProductVariants.Any(r =>
                            r.ProductVariantId == oi.ProductVariantId && r.TotalRefunded >= oi.Quantity)) // Ürünün satın alınan adedi kadar iade talebi yapılabilir
                    .Select(oi => new
                    {
                        oi.ProductVariantId, // ProductVariantId
                        ProductName = oi.ProductVariant.Product.Name, // Ürün adı
                        oi.Quantity, // Ürün adedi
                        Price = oi.ProductVariant.Product.Price // Ürün fiyatı
                    })
                    .ToList(),
                paymentMethod = order.PaymentMethod?.PaymentToken // Ödeme token'ı, null kontrolü
            };

            return Ok(response);
        }


        [HttpPost]
        public async Task<IActionResult> CreateRefund([FromBody] CreateRefundRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Sipariş ve ödeme bilgilerini getir
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.PaymentMethod)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return BadRequest(new { success = false, message = "Sipariş bulunamadı" });
                }

                // 2. Kapıda Ödeme kontrolü
                if (order.PaymentMethod?.PaymentToken == "Kapıda Ödeme")
                {
                    if (string.IsNullOrWhiteSpace(request.Iban))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Kapıda ödeme iadeleri için IBAN bilgisi zorunludur"
                        });
                    }

                    if (string.IsNullOrWhiteSpace(request.FullName))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Kapıda ödeme iadeleri için Ad Soyad bilgisi zorunludur"
                        });
                    }
                }

                // 3. RefundCode oluşturma
                // Örnek: RefundCode formatı: REFUND-YYYYMMDD-HHMMSS-xxxxxx (GUID)
                var refundCode = $"REF-{Guid.NewGuid().ToString().Substring(0, 4)}{Guid.NewGuid().ToString().Substring(0, 6)}";
                // Veritabanında var mı diye kontrol et
                var existingCode = _context.RefundRequests.FirstOrDefault(r => r.RefundCode == refundCode);
                if (existingCode != null)
                {
                    // Eğer varsa, yeni bir kod oluştur
                    refundCode = $"REF-{Guid.NewGuid().ToString().Substring(0, 4)}{Guid.NewGuid().ToString().Substring(0, 6)}";
                }
                // 4. İade talebini oluştur
                var refundRequest = new RefundRequest
                {
                    OrderId = order.OrderId,
                    PaymentMethodId = order.PaymentMethod.PaymentMethodId,
                    Iban = order.PaymentMethod.PaymentToken == "Kapıda Ödeme" ? request.Iban : null,
                    fullName = order.PaymentMethod.PaymentToken == "Kapıda Ödeme" ? request.FullName : null,
                    TotalPrice = 0,
                    RefundStatus = "Beklemede",
                    RefundRequestDate = DateTime.Now,
                    RefundCode = refundCode  // RefundCode'u ekliyoruz
                };

                _context.RefundRequests.Add(refundRequest);
                await _context.SaveChangesAsync();

                // 5. İade edilen ürünleri işle
                foreach (var item in request.Items)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductVariantId == item.ProductVariantId);
                    if (orderItem == null)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = "Siparişte geçersiz ürün" });
                    }

                    var refundedItem = new RefundedItem
                    {
                        ProductVariantId = item.ProductVariantId,
                        Quantity = item.Quantity,
                        TotalPrice = item.UnitPrice * item.Quantity,
                        RefundRequestId = refundRequest.RefundRequestId,
                        ReasonType = item.ReasonType == "Diğer" ? item.Reason : item.ReasonType
                    };

                    refundRequest.TotalPrice += refundedItem.TotalPrice;
                    _context.RefundedItems.Add(refundedItem);
                }

                _context.RefundRequests.Update(refundRequest);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    refundRequestId = refundRequest.RefundRequestId,
                    refundCode = refundRequest.RefundCode, // RefundCode'u de geri döndürüyoruz
                    message = "İade talebi başarıyla oluşturuldu"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "İade talebi oluşturulamadı",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        public class CreateRefundRequestDto
        {
            public int OrderId { get; set; }
            public int PaymentMethodId { get; set; }
            public string Iban { get; set; } // Kapıda ödeme için
            public string FullName { get; set; } // Kapıda ödeme için
            public List<RefundItemDto> Items { get; set; }
        }

        public class RefundItemDto
        {
            public int ProductVariantId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string Reason { get; set; } // "Diğer" seçilirse textarea'daki değer
            public string ReasonType { get; set; } // Seçilen sebep ("Yanlış Ürün", "Kusurlu Ürün" vb.)
        }
        public async Task<ActionResult> GetMyRefunds()
        {
            // 1. Login olan kullanıcının ID'sini al
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized("Kullanıcı bilgisi alınamadı");
            }

            // 2. Sadece bu kullanıcının iadelerini çek
            var refunds = await _context.RefundRequests
                .Include(r => r.RefundedItems)
                .Where(r => r.Order.UserId == currentUserId) // Burada artık parametre yok!
                .OrderByDescending(r => r.RefundRequestDate)
                .Select(r => new
                {
                    r.RefundRequestId,
                    r.OrderId,
                    r.RefundRequestDate,
                    ItemCount = r.RefundedItems.Count,
                    r.TotalPrice,
                    r.RefundStatus,
                    r.Order.OrderCode,
                    r.RefundCode
                })
                .ToListAsync();

            return Ok(refunds);
        }
        public IActionResult List()
        {
            return View();
        }
        public IActionResult Details()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetRefundDetails(string id)
        {
            try
            {
                Console.WriteLine($"Refund detayları getiriliyor. RefundCode: {id}");

                // 1. Önce temel refund bilgisini çekelim (IBAN bilgisini almak için)
                var refund = await _context.RefundRequests
                    .FirstOrDefaultAsync(r => r.RefundCode == id);

                if (refund == null)
                {
                    Console.WriteLine($"Refund bulunamadı. RefundCode: {id}");
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

                // 3. RefundedItems bilgisini RefundRequestId ile çekelim
                var refundedItems = await _context.RefundedItems
                    .Where(ri => ri.RefundRequestId == refund.RefundRequestId) // Burada RefundRequestId kullanıyoruz
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
                        orderCode = order?.OrderCode,
                        orderDate = order?.OrderDate ?? DateTime.MinValue,
                        totalAmount = order?.TotalPrice ?? 0,
                        paymentMethod = order?.PaymentMethod?.PaymentToken ?? "Bilinmiyor"
                    },
                    refund = new
                    {
                        status = refund.RefundStatus ?? "Bilinmiyor",
                        iban = iban, // Sadece Kapıda Ödeme ve IBAN varsa dolu olacak
                        fullName = fullname,
                        requestDate = refund.RefundRequestDate
                    },
                    items = refundedItems?.Select(ri => new
                    {
                        productName = ri.ProductVariant?.Product?.Name ?? "Bilinmiyor",
                        productCode = ri.ProductVariant?.Product?.ProductCode ?? "Bilinmiyor",
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
                Console.WriteLine($"HATA: Refund detayları getirilirken hata oluştu. RefundCode: {id}");
                Console.WriteLine($"Hata detayı: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                return StatusCode(500, "İşlem sırasında bir hata oluştu. Lütfen sistem yöneticinize başvurun.");
            }
        }
    }
}
