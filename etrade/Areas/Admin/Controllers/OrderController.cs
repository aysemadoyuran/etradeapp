using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Shop.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class OrderController : Controller
    {
        private readonly EtradeContext _context;

        public OrderController(IHttpContextAccessor httpContextAccessor)
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
            return View();
        }
        public IActionResult Details(int id)
        {
            // Veritabanından sipariş detaylarını almak için gerekli işlemleri yapıyoruz
            ViewData["OrderId"] = id;// orderId'yi ViewData ile gönderiyoruz
            return View();
        }

        public async Task<IActionResult> GetDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.ProductVariant)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.ShippingAddress)
                        .ThenInclude(sa => sa.Ilce)
                    .Where(o => o.OrderId == id)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderStatus,
                        o.OrderDate,
                        o.TotalPrice,
                        PaymentMethod = o.PaymentMethod == null ? null : new
                        {
                            o.PaymentMethod.PaymentMethodId,
                            o.PaymentMethod.PaymentMethodType,
                            o.PaymentMethod.PaymentStatus,
                            o.PaymentMethod.Amount,
                            o.PaymentMethod.PaymentDate,
                            o.PaymentMethod.PaymentToken
                        },
                        ShippingAddress = o.ShippingAddress == null ? null : new
                        {
                            o.ShippingAddress.NameSurname,
                            Il = o.ShippingAddress.Il != null ? o.ShippingAddress.Il.Ad : "Bilinmiyor",  // null kontrolü
                            Ilce = o.ShippingAddress.Ilce != null ? o.ShippingAddress.Ilce.Ad : "Bilinmiyor",
                            Semt = o.ShippingAddress.District != null ? o.ShippingAddress.District.SemtAdi : "Bilinmiyor",
                            Mahalle = o.ShippingAddress.Street != null ? o.ShippingAddress.Street.MahalleAdi : "Bilinmiyor",
                            AcikAdres = o.ShippingAddress.AcikAdres != null ? o.ShippingAddress.AcikAdres : "Bilinmiyor"
                        },
                        OrderItems = o.OrderItems.Select(oi => new
                        {
                            oi.OrderItemId,
                            oi.Quantity,
                            oi.Price,
                            Product = oi.ProductVariant != null ? new
                            {
                                Name = oi.ProductVariant.Product != null ? oi.ProductVariant.Product.Name : "Bilinmiyor",  // null kontrolü
                                Size = oi.ProductVariant.Size != null ? oi.ProductVariant.Size.Name : "Bilinmiyor",  // Size kontrolü
                                Color = oi.ProductVariant.Color != null ? oi.ProductVariant.Color.Name : "Bilinmiyor"  // Color kontrolü
                            } : null
                        })
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound();
                }
                ViewData["OrderStatus"] = order.OrderStatus;
                return Json(order);
            }
            catch (Exception ex)
            {
                // Hata mesajını döndür
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        // Sipariş İstatistiklerini Döndüren API Metodu
        [HttpGet]
        public async Task<IActionResult> GetOrderStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var todayOrders = await _context.Orders.CountAsync(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == DateTime.Today);
            var last7DaysOrders = await _context.Orders
                .Where(o => o.OrderDate >= DateTime.Today.AddDays(-7))
                .CountAsync();
            var deliveredOrders = await _context.Orders
                .Where(o => o.OrderStatus == "Teslim Edildi")
                .CountAsync();

            return Ok(new
            {
                totalOrders,
                todayOrders,
                last7DaysOrders,
                deliveredOrders
            });
        }

        // Siparişleri filtreleyerek getiren API metodu
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { TotalOrders = totalOrders, Orders = orders });
        }
        public List<Order> GetOrdersByStatus(string status)
        {
            switch (status.ToLower())
            {
                case "pending":
                    return _context.Orders.Where(o => o.OrderStatus == "Bekleyen").ToList();
                case "approved":
                    return _context.Orders.Where(o => o.OrderStatus == "Onaylanan").ToList();
                case "prepared":
                    return _context.Orders.Where(o => o.OrderStatus == "Hazırlanan").ToList();
                case "shipped":
                    return _context.Orders.Where(o => o.OrderStatus == "Kargoya Verildi").ToList();
                case "cancelled":
                    return _context.Orders.Where(o => o.OrderStatus == "İptal Edilenler").ToList();
                default:
                    return new List<Order>();
            }
        }
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            // Geçerli durumlar ve geçiş kuralları
            var validStatuses = new List<string> { "Onaylandı", "Hazırlanıyor", "Kargoya Verildi", "Teslim Edildi" };

            // Durum geçerli değilse, işlemi engelle ve kullanıcıya hata mesajı vermeden işlem yapma
            if (string.IsNullOrEmpty(newStatus) || !validStatuses.Contains(newStatus))
            {
                return Ok(new { message = "Durum güncellemesi başarıyla yapıldı." });
            }

            // Siparişi bul
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
            {
                return Ok(new { message = "Durum güncellemesi başarıyla yapıldı." });
            }

            // Siparişin mevcut durumu
            string oldStatus = order.OrderStatus;

            // Durum geçişini kontrol et: belirli bir sırada ilerleme olmalı
            if (oldStatus == "Onaylandı" && newStatus != "Hazırlanıyor")
            {
                return Ok(new { message = "Durum geçişi başarıyla yapıldı." });
            }

            if (oldStatus == "Hazırlanıyor" && newStatus != "Kargoya Verildi")
            {
                return Ok(new { message = "Durum geçişi başarıyla yapıldı." });
            }

            if (oldStatus == "Kargoya Verildi" && newStatus != "Teslim Edildi")
            {
                return Ok(new { message = "Durum geçişi başarıyla yapıldı." });
            }

            // Eğer yeni durum eski duruma eşitse, işlem yapma
            if (oldStatus == newStatus)
            {
                return Ok(new { message = "Durum zaten aynı, değişiklik yapılmadı." });
            }

            // Durum geçişi geçerli ise işlemi yap
            try
            {
                // Durum güncellemesi
                order.OrderStatus = newStatus;
                order.UpdateDate = DateTime.Now;
                _context.Update(order);

                // Siparişin ürünleri üzerinde işlem yapıyorsanız, her ürün için ilgili güncellemeyi yapın
                foreach (var item in order.OrderItems)
                {
                    // Örneğin, her ürünün durumu da güncellenebilir
                    // item.ProductStatus = newStatus;  // Örnek: Ürün durumunu güncelleme
                }

                await _context.SaveChangesAsync();
                var message = newStatus == "Teslim Edildi"
                    ? $"#{order.OrderCode} numaralı siparişiniz teslim edildi."
                    : $"#{order.OrderCode} numaralı siparişinizin durumu '{newStatus}' olarak güncellenmiştir.";

                // Bildirim oluşturma
                var notification = new Notification
                {
                    UserId = order.UserId, // Siparişin sahibine bildirim gönderiyoruz
                    Title = "Sipariş Durumu Güncellendi",
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    IsGlobal = false, // Kullanıcıya özel
                    NotificationType = "Order" // Bildirim türü (siparişle ilgili)
                };

                // Bildirimi veritabanına kaydet
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                if (newStatus == "Teslim Edildi")
                {
                    var userCoin = await _context.UserCoins.FirstAsync(x => x.UserId == order.UserId);
                    var earnedCoin = Math.Round(order.TotalPrice * 0.01m, 2);
                    userCoin.Coin += earnedCoin;
                    userCoin.LastUpdated = DateTime.Now;

                    _context.UserCoins.Update(userCoin);

                    // Coin bildirimi oluştur
                    var coinNotification = new Notification
                    {
                        UserId = order.UserId,
                        Title = "Para Puan Kazandınız!",
                        Message = $"#{order.OrderCode} numaralı siparişinizden {earnedCoin} para puan kazandınız.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        IsGlobal = false,
                        NotificationType = "Coin"
                    };

                    _context.Notifications.Add(coinNotification);
                    await _context.SaveChangesAsync();
                }

                // Başarılı işlem mesajı
                return Ok(new { message = "Durum güncellemesi başarıyla yapıldı." });
            }
            catch (Exception ex)
            {
                // Hata durumunda sadece işlem engelleniyor, kullanıcıya hata mesajı verilmiyor
                return Ok(new { message = "Durum güncellemesi başarıyla yapıldı." });
            }
        }

        [HttpPost]
        [Route("Admin/Order/CancelOrder/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.PaymentMethod)
            .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound("Sipariş bulunamadı");
            }

            try
            {
                if (order.PaymentStatus == "Kapıda Ödeme")
                {
                    // Kapıda ödeme ise sadece siparişi iptal et
                    await UpdateStockAfterCancel(order);  // Stokları güncelle
                    order.OrderStatus = "İptal Edildi";
                    _context.SaveChanges();
                    return Ok("Sipariş başarıyla iptal edildi");
                }
                else if (order.PaymentStatus == "Ödendi")
                {
                    // İyzico ödeme varsa ödeme iadesini yap
                    var paymentId = order.PaymentMethod.PaymentToken; // İyzico'dan alınan transactionId

                    // İyzico iade işlemi
                    //Sorun burada
                    var refundResult = RefundPaymentByAmountAsync(paymentId, order.TotalPrice).Result;
                    Console.WriteLine($"Ödeme için paymentId: {paymentId}");
                    if (refundResult.Status != "success")
                    {
                        return StatusCode(500, "İade işlemi başarısız oldu: " + refundResult.ErrorMessage);
                    }

                    // İyzico'dan ödeme iadesi başarılıysa siparişi iptal et
                    await UpdateStockAfterCancel(order);  // Stokları güncelle
                    order.OrderStatus = "İptal Edildi";
                    _context.SaveChanges();

                    return Ok("Sipariş ve ödeme iade işlemi başarıyla tamamlandı");
                }
                else
                {
                    return BadRequest("Bilinmeyen ödeme yöntemi");
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda log kaydı yapabiliriz
                Console.WriteLine($"Hata: {ex.Message}");
                return StatusCode(500, "Bir hata oluştu: " + ex.Message);
            }
        }

        // Sipariş iptal edilince ürünlerin stoklarını geri artıran metod
        private async Task UpdateStockAfterCancel(Order order)
        {
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                Console.WriteLine("Siparişin içinde ürün bulunamadı.");
                return; // Ürün yoksa hiçbir işlem yapma
            }

            var inventoryService = new InventoryService(_context);  // InventoryService sınıfını oluşturuyoruz

            foreach (var orderItem in order.OrderItems)
            {
                var product = await _context.ProductVariants
                                            .FirstOrDefaultAsync(p => p.Id == orderItem.ProductVariantId);
                if (product != null)
                {
                    // Stok hareketini kaydediyoruz
                    await inventoryService.UpdateStockAsync(
                        product.Id, // Ürün varyantı ID'si
                        orderItem.Quantity, // Siparişteki ürün adedi
                        "Sipariş yönetici tarafından iptal edildi", // Açıklama
                        "Giriş", // Hareket tipi (stoka ekleme)
                        product.Stock // Güncel stok durumu
                    );
                }
            }

            await _context.SaveChangesAsync();  // Değişiklikleri topluca kaydet
        }
        public async Task<Refund> RefundPaymentByAmountAsync(string paymentId, decimal amount)
        {
            if (string.IsNullOrEmpty(paymentId))
            {
                throw new ArgumentNullException(nameof(paymentId), "Payment ID cannot be null or empty.");
            }
            var setting = _context.StoreSettings.FirstOrDefault();

            if (setting == null)
            {
                // Hata yönetimi
                throw new Exception("Store ayarları bulunamadı.");
            }
            var apiKey = EncryptionHelper.Decrypt(setting.IyzicoApiKey);
            var secretKey = EncryptionHelper.Decrypt(setting.IyzicoSecretKey);
            var baseUrl = setting.IyzicoBaseUrl;
            Options options = new Options
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                BaseUrl = baseUrl
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