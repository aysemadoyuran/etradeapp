using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using Microsoft.AspNetCore.Identity;
using etrade.Areas.Shop.Services;
using iText.Layout.Borders;
using iText.Kernel.Geom;
using static etrade.Areas.Tenant.Controllers.StoreController;
using etrade.Areas.Admin.Services;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]


    public class OrderController : Controller
    {
        private readonly EtradeContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly InventoryService _inventoryService;
        private readonly CachedStoreSettingsService _settingsService;



        public OrderController(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager, InventoryService inventoryService, CachedStoreSettingsService settingsService)
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
            _inventoryService = inventoryService;
            _userManager = userManager;
            _settingsService = settingsService;


        }
        public async Task<IActionResult> CreateOrder()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderId = HttpContext.Session.GetInt32("OrderId");


            // Kullanıcının adreslerini veritabanından al
            var userAddresses = await _context.Address
                .Where(a => a.UserId == userId)  // Kullanıcının adreslerini çekiyoruz
                .ToListAsync();

            var basket = await _context.Baskets
                .Where(b => b.UserId == userId && b.IsActive)  // Kullanıcıya ait aktif sepeti al
                .Include(b => b.ItemsBaskets)  // Sepetteki ürünleri de yükle
                    .ThenInclude(item => item.ProductVariant)  // Ürün variantlarını yükle
                        .ThenInclude(variant => variant.Product)  // Ürün bilgilerini yükle
                .FirstOrDefaultAsync();
            // İlk bulunan sepeti al
            decimal baskettotalPrice = basket.ItemsBaskets
                .Sum(item => item.Quantity * item.ProductVariant.Product.Price);
            ViewBag.BasketTotalPrice = baskettotalPrice;



            // Sepetteki aktif olmayan ürünleri al
            if (basket != null && basket.ItemsBaskets != null)
            {
                var inactiveItems = basket.ItemsBaskets.Where(item => !item.IsActive).ToList();  // IsActive false olanları al

                if (inactiveItems.Any())
                {
                    // Aktif olmayan ürünlerle işlem yapabilirsiniz
                    // Örneğin, order ve orderitem kayıtlarını burada işleyeceğiz.
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

            var order = _context.Orders
                .FirstOrDefault(x => x.OrderId == orderId && x.UserId == userId); // veya session’dan alıyorsan

            decimal totalPrice = order.TotalPrice;

            // Sipariş modelini oluştur
            var model = new OrderViewModel
            {
                OrderId = orderId.Value,
                UserAddresses = userAddresses,  // Kullanıcının adreslerini modelimize ekliyoruz
                BasketItems = basket.ItemsBaskets.ToList(),  // Sepetteki ürünleri modelimize ekliyoruz
                TotalPrice = totalPrice  // Toplam fiyatı modelimize ekliyoruz
            };

            return View(model);  // Modeli view'a gönderiyoruz
        }
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string couponCode, Guid orderId, decimal currentTotal)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 1. Kuponu çek
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code == couponCode &&
                        c.IsActive
                    );

                if (coupon == null)
                {
                    return Json(new { success = false, message = "Geçersiz veya süresi dolmuş kupon kodu." });
                }

                // 2. Kullanıcı bu kuponu daha önce kullanmış mı?
                bool hasUsedCoupon = await _context.CouponUsages
                    .AnyAsync(cu =>
                        cu.UserId == userId &&
                        cu.CouponId == coupon.CouponId &&
                        cu.IsUsed
                    );

                if (hasUsedCoupon)
                {
                    return Json(new { success = false, message = "Bu kuponu zaten kullandınız." });
                }

                // 3. Kupon kategorisi kontrolleri
                if (coupon.CouponCategory == "LoyalCustomer")
                {
                    int orderCount = await _context.Orders
                        .CountAsync(o => o.UserId == userId);

                    if (orderCount < 5)
                    {
                        return Json(new { success = false, message = "Bu kuponu yalnızca en az 5 sipariş vermiş müşteriler kullanabilir." });
                    }
                }

                // 4. Minimum tutar kontrolü
                if (currentTotal < coupon.MinimumOrderAmount)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Bu kuponu kullanabilmek için minimum {coupon.MinimumOrderAmount:N2} TL tutarında alışveriş yapmalısınız."
                    });
                }

                // 5. İndirim hesapla
                decimal discountAmount = coupon.DiscountValue;
                decimal newTotal = currentTotal - discountAmount;
                if (newTotal < 0) newTotal = 0;

                // 6. Session'a kaydet
                HttpContext.Session.SetString("DiscountedTotal", newTotal.ToString("F2"));
                HttpContext.Session.SetString("AppliedCouponCode", coupon.Code);
                HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString("F2"));

                // 7. Başarıyla dön
                return Json(new
                {
                    success = true,
                    message = "Kupon başarıyla uygulandı.",
                    discountAmount,
                    newTotal,
                    couponCode = coupon.Code
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Kupon uygulanırken bir hata oluştu." });
            }
        }
        [HttpPost]
        public IActionResult SaveAddress(AddressViewModel model)

        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz veri!" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Kullanıcı ID'sini alın

                var address = new Entity.Address
                {
                    UserId = userId,
                    NameSurname = model.NameSurname,
                    Title = model.AddressTitle,
                    IlId = model.CityId,
                    IlceId = model.DistrictId,
                    SemtId = model.NeighborhoodId,
                    MahalleId = model.VillageId,
                    Telefon = model.PhoneNumber,
                    AcikAdres = model.AddressDetail,
                };

                _context.Add(address);
                _context.SaveChanges();

                return RedirectToAction("CreateOrder"); // Başka bir sayfaya yönlendirebilirsiniz
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Adres kaydedilirken sunucu hatası oluştu." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CompleteOrder([FromBody] OrderViewModel orderRequest)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orderId = HttpContext.Session.GetInt32("OrderId");

                if (!orderId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Sipariş ID'si bulunamadı." });
                }

                // Seçilen adresin var olup olmadığını kontrol et
                var selectedAddressId = orderRequest.SelectedAddressId;
                var address = _context.Address
                                       .Where(a => a.Id == selectedAddressId)
                                       .FirstOrDefault();
                if (address == null)
                {
                    return BadRequest(new { success = false, message = "Geçersiz adres ID'si." });
                }

                // Siparişi al
                var order = _context.Orders.FirstOrDefault(o => o.OrderId == orderId.Value);
                if (orderRequest == null || order == null)
                {
                    return BadRequest(new { success = false, message = "Sipariş veya veri hatası." });
                }

                // Kupon kodu var mı? Eğer varsa, kuponu güncelle
                if (!string.IsNullOrEmpty(orderRequest.CouponCode))
                {
                    // Kupon koduna ait ID'yi bul
                    var coupon = await _context.Coupons
                        .FirstOrDefaultAsync(c => c.Code == orderRequest.CouponCode);

                    if (coupon != null)
                    {
                        // Kuponun ID'sini alarak CouponUsage tablosunda güncelleme yap
                        var couponUsage = await _context.CouponUsages
                            .FirstOrDefaultAsync(cu => cu.CouponId == coupon.CouponId && cu.UserId == userId);

                        if (couponUsage != null)
                        {
                            // Kuponu kullandı olarak işaretle
                            couponUsage.IsUsed = true;
                            _context.Update(couponUsage);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = "Kupon kullanım kaydı bulunamadı." });
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Geçersiz kupon kodu." });
                    }
                }

                string discountedTotalStr = HttpContext.Session.GetString("DiscountedTotal");
                if (!string.IsNullOrEmpty(discountedTotalStr) && decimal.TryParse(discountedTotalStr, out decimal discountedTotal))
                {
                    order.TotalPrice = discountedTotal;
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }

                // Ödeme işlemleri
                if (orderRequest.PaymentMethod == "cash")
                {
                    // Kapıda ödeme işlemleri
                    var paymentMethod = new PaymentMethod
                    {
                        OrderId = orderId.Value,
                        PaymentStatus = "Kapıda Ödeme",
                        Amount = order.TotalPrice,
                        PaymentDate = DateTime.Now,
                        PaymentMethodType = "Kapıda Ödeme",
                        PaymentToken = "Kapıda Ödeme"
                    };

                    _context.PaymentMethods.Add(paymentMethod);
                    await _context.SaveChangesAsync();

                    order.ShippingAddressId = orderRequest.SelectedAddressId;
                    order.PaymentStatus = paymentMethod.PaymentStatus;
                    order.OrderStatus = "Beklemede";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // İyzico ödeme işlemi
                    var paymentResult = await CompletePaymentWithIyzico(order.OrderId, orderRequest.CardHolderName, orderRequest.CardNumber, orderRequest.ExpireMonth, orderRequest.ExpireYear, orderRequest.Cvc, orderRequest.TotalPrice.GetValueOrDefault(0));

                    if (!paymentResult.IsSuccess)
                    {
                        return BadRequest(new { success = false, message = "Ödeme işlemi başarısız." });
                    }

                    var paymentMethod = new PaymentMethod
                    {
                        OrderId = orderId.Value,
                        PaymentStatus = "Ödendi",
                        Amount = order.TotalPrice,
                        PaymentDate = DateTime.Now,
                        PaymentMethodType = "Kredi Kartı",
                        PaymentToken = paymentResult.PaymentToken
                    };

                    _context.PaymentMethods.Add(paymentMethod);
                    await _context.SaveChangesAsync();

                    order.ShippingAddressId = orderRequest.SelectedAddressId;
                    order.PaymentStatus = paymentMethod.PaymentStatus;
                    order.OrderStatus = "Beklemede";
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }

                // Sepet öğelerini al
                var basketItems = _context.ItemsBaskets.Where(b => b.Basket.UserId == userId).ToList();
                if (basketItems.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Sepet boş, işlem yapılamaz." });
                }

                // Sepet öğelerini silmeden önce stokları güncelle
                foreach (var item in basketItems)
                {
                    var productVariant = _context.ProductVariants.FirstOrDefault(pv => pv.Id == item.ProductVariantId);
                    if (productVariant != null)
                    {
                        var quantityChanged = item.Quantity; // Sepetteki ürün adeti kadar stok düşürülüyor
                                                             // Stok güncelleme işlemi yapalım
                        await _inventoryService.UpdateStockAsync(productVariant.Id, -quantityChanged, "Ürün Satışı", "Çıkış", productVariant.Stock);
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "Ürün varyantı bulunamadı." });
                    }
                }

                // Sepet öğelerini sil
                _context.ItemsBaskets.RemoveRange(basketItems);
                await _context.SaveChangesAsync();

                // Kullanıcıya ait "Taslak" ve "Ödenmedi" siparişlerini sil
                var userOrders = _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Taslak" && o.PaymentStatus == "Ödenmedi")
                .ToList();

                foreach (var orderToDelete in userOrders)
                {
                    var orderItems = _context.OrderItems.Where(oi => oi.OrderId == orderToDelete.OrderId).ToList();
                    _context.OrderItems.RemoveRange(orderItems);  // OrderItems'ı sil
                    _context.Orders.Remove(orderToDelete);
                }

                await _context.SaveChangesAsync();

                HttpContext.Session.Remove("DiscountedTotal");
                return Ok(new { success = true, message = "Sipariş başarıyla tamamlandı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Sipariş tamamlanırken bir hata oluştu.", error = ex.Message });
            }
        }
        private async Task<(bool IsSuccess, string ErrorMessage, string PaymentToken)> CompletePaymentWithIyzico(
          int orderId, string cardHolderName, string cardNumber, string expireMonth, string expireYear, string cvc, decimal totalPrice)
        {
            try
            {
                // Siparişi veritabanından al
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null)
                {
                    return (false, "Sipariş bulunamadı.", null);
                }

                // İyzico ödeme isteğini hazırla
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

                CreatePaymentRequest request = new CreatePaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = orderId.ToString(),
                    Price = order.TotalPrice.ToString("F2", CultureInfo.InvariantCulture),
                    PaidPrice = order.TotalPrice.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = Currency.TRY.ToString(),
                    Installment = 1,
                    PaymentChannel = PaymentChannel.WEB.ToString(),
                    PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                    PaymentCard = new PaymentCard
                    {
                        CardHolderName = cardHolderName,
                        CardNumber = cardNumber,
                        ExpireMonth = expireMonth,
                        ExpireYear = expireYear,
                        Cvc = cvc,
                        RegisterCard = 0 // Kredi kartı kaydetme seçeneği
                    },

                    Buyer = new Buyer
                    {
                        Id = orderId.ToString(),
                        Name = cardHolderName,
                        Surname = "Soyad", // Rastgele bir soyad
                        Email = "randomemail@example.com", // Rastgele e-posta
                        IdentityNumber = "12345678901", // Rastgele T.C. kimlik numarası
                        RegistrationAddress = "Test Adresi", // Rastgele adres
                        Ip = "192.168.1.1", // Rastgele IP adresi
                        City = "İstanbul", // Rastgele şehir
                        Country = "Türkiye" // Rastgele ülke
                    },

                    BillingAddress = new Iyzipay.Model.Address
                    {
                        ContactName = "John Doe",
                        City = "İstanbul",
                        Country = "Türkiye",
                        Description = "Nişantaşı, Şişli, İstanbul",
                        ZipCode = "34000",

                    },
                };

                request.BasketItems = new List<BasketItem>
                {
                 new BasketItem
                   {
                     Id = "1", // Ürün ID'si
                    Name = "Ürün Adı", // Ürün adı
                    Category1 = "Kategori", // Ürün kategorisi
                    Category2 = "Alt Kategori", // Alt kategori (isteğe bağlı)
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = totalPrice.ToString("F2")                    
                     // Fiziksel ürün
                   }
               };

                // Shipping address zorunlu alandı, burada doldurulmuş
                request.ShippingAddress = new Iyzipay.Model.Address
                {
                    ContactName = "John Doe",
                    City = "İstanbul",
                    Country = "Türkiye",
                    Description = "Nişantaşı, Şişli, İstanbul",
                    ZipCode = "34000",

                };

                Payment payment;
                try
                {
                    // Ödeme işlemini başlat
                    payment = await Payment.Create(request, options);
                }
                catch (Exception ex)
                {
                    return (false, $"Ödeme işlemi başlatılırken hata oluştu: {ex.Message}", null);
                }

                // Ödeme yanıtı null ise
                if (payment == null)
                {
                    return (false, "Ödeme yanıtı alınamadı. İyzico yanıtı: " + JsonConvert.SerializeObject(request), null);
                }

                // Ödeme başarılı mı kontrol et
                if (payment.Status == "success")
                {
                    string paymentToken = payment.PaymentId;

                    try
                    {
                        // Sipariş güncelleme
                        order.PaymentStatus = "Ödendi";
                        order.OrderStatus = "Onaylandı";
                        _context.Update(order);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Ödeme başarılı ancak sipariş güncellenirken hata oluştu: {ex.Message}", paymentToken);
                    }
                    return (true, "Ödeme başarılı.", paymentToken);
                }
                else
                {
                    // Eğer ödeme başarısızsa detaylı hata mesajı döndür
                    string errorDetails = $"Ödeme başarısız: {payment.ErrorMessage}, Detay: {payment.Status}, Hata Kodu: {payment.ErrorCode}";
                    return (false, errorDetails, null);
                }
            }
            catch (Exception ex)
            {
                // Genel hata yakalama
                return (false, $"Genel hata oluştu: {ex.Message}", null);
            }
        }
        [HttpGet]
        public IActionResult OrderSuccess(int orderId)
        {

            return View();
        }
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            // Kullanıcıya ait siparişleri alıyoruz ve OrderItem'ların sayısını hesaplıyoruz
            var orders = _context.Orders
                .Where(o => o.UserId == userId)  // Kullanıcıya ait siparişleri alıyoruz
                .Include(o => o.ShippingAddress)  // ShippingAddress bilgilerini dahil ediyoruz
                .Include(o => o.OrderItems)  // OrderItems (sipariş ürünlerini) dahil ediyoruz
                .OrderByDescending(o => o.OrderId)  // OrderId'ye göre büyükten küçüğe sıralıyoruz
                .ToList()
                .Select(o => new OrderListViewModel
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    OrderDateFormatted = o.OrderDate.HasValue
    ? TimeZoneInfo.ConvertTimeFromUtc(o.OrderDate.Value, TimeZoneInfo.Local).ToString("dd MM yyyy HH:mm")
    : "Geçersiz Tarih",
                    TotalPrice = o.TotalPrice,
                    OrderStatus = o.OrderStatus ?? "Durum Bilgisi Yok",
                    PaymentStatus = o.PaymentStatus ?? "Ödeme Durumu Yok",
                    AlıcıAdSoyad = o.ShippingAddress?.NameSurname ?? "Adres Yok",
                    OrderItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                    UpdateDate = o.UpdateDate
                    // Siparişin içerisindeki ürünlerin toplam miktarını alıyoruz
                })
                .ToList();
            var ordersToDelete = _context.Orders
           .Where(o => o.OrderStatus == "Taslak" && o.PaymentStatus == "Ödenmedi")
           .ToList();  // "Taslak" ve "Ödenmedi" durumundaki siparişleri buluyoruz

            if (ordersToDelete.Any())
            {
                foreach (var order in ordersToDelete)
                {
                    // İlgili OrderItems'leri sil
                    var orderItemsToDelete = _context.OrderItems
                        .Where(oi => oi.OrderId == order.OrderId);

                    _context.OrderItems.RemoveRange(orderItemsToDelete); // Sipariş item'larını sil

                    _context.Orders.Remove(order); // Siparişi sil
                }

                _context.SaveChanges(); // Veritabanındaki değişiklikleri kaydet
            }

            return View(orders);
        }
        public IActionResult Details(string orderCode)
        {
            // OrderCode üzerinden siparişi alıyoruz
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                .Include(o => o.PaymentMethod)
                .FirstOrDefault(o => o.OrderCode == orderCode);  // OrderCode ile sorgulama yapıyoruz

            if (order == null)
            {
                return NotFound();  // Eğer sipariş bulunamazsa, 404 döndürüyoruz
            }

            // RefundRequest tablosunda "ParaIadesiYapildi" durumunda bir kayıt var mı kontrol ediyoruz
            var refundRequest = _context.RefundRequests
                .Include(r => r.Order)  // Order ilişkisini dahil ediyoruz
                .FirstOrDefault(r => r.OrderId == order.OrderId && r.RefundStatus == "ParaIadesiYapildi");

            Dictionary<int, int> refundedItems = new Dictionary<int, int>();

            if (refundRequest != null)
            {
                // RefundRequest için iade edilen ürünleri alıyoruz
                var refundItems = _context.RefundedItems
                    .Where(ri => ri.RefundRequestId == refundRequest.RefundRequestId)
                    .ToList();

                foreach (var item in refundItems)
                {
                    refundedItems[item.ProductVariantId] = item.Quantity;
                }
            }

            // Siparişin detaylarını ViewModel'e aktarıyoruz
            var orderDetails = new OrderDetailsViewModel
            {
                OrderCode = order.OrderCode,  // OrderCode'u kullanıyoruz
                UserName = order.User.UserName,
                OrderDate = order.OrderDate.ToString(),
                OrderStatus = order.OrderStatus.ToString(),
                ShippingAddress = new ShippingAddressViewModel
                {
                    Address = order.ShippingAddress.AcikAdres,
                    NameSurname = order.ShippingAddress.NameSurname,
                    Phone = order.ShippingAddress.Telefon
                },
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductVariant.Product.Id,
                    ProductName = oi.ProductVariant?.Product?.Name ?? "Bilinmiyor",
                    Size = oi.ProductVariant?.Size?.Name ?? "Bilinmiyor",
                    Color = oi.ProductVariant?.Color?.Name ?? "Bilinmiyor",
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    TotalPrice = oi.TotalPrice,
                    RefundedQuantity = refundedItems.ContainsKey(oi.ProductVariantId) ? refundedItems[oi.ProductVariantId] : 0
                }).ToList(),
                PaymentMethod = order.PaymentMethod.PaymentMethodType,
                TotalPrice = order.TotalPrice
            };

            return View(orderDetails);  // Detayları View'e gönderiyoruz
        }
        public IActionResult GenerateInvoice(string orderCode)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                .Include(o => o.PaymentMethod)
                .FirstOrDefault(o => o.OrderCode == orderCode);

            if (order == null)
            {
                return NotFound();
            }

            // StoreSettings'den JSON bilgilerini al
            var storeSettings = _context.StoreSettings.FirstOrDefault();
            if (storeSettings == null)
            {
                return NotFound("Store settings not found");
            }

            var jsonFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storeSettings.JsonFilePath.TrimStart('/'));
            Console.WriteLine($"Trying to read JSON file from: {jsonFilePath}"); // Add logging
            if (!System.IO.File.Exists(jsonFilePath))
            {
                return NotFound($"JSON file not found at: {jsonFilePath}");
            }

            var jsonContent = System.IO.File.ReadAllText(jsonFilePath);
            var storeInfo = JsonConvert.DeserializeObject<StoreInfo>(jsonContent);
            string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "pdf", $"Invoice_{orderCode}.pdf");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));

            using (var writer = new PdfWriter(filePath))
            using (var pdf = new PdfDocument(writer))
            {
                var document = new Document(pdf, PageSize.A4);
                document.SetMargins(40, 30, 40, 30);

                // Font ayarları
                string fontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arial.ttf");
                PdfFont boldFont = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
                PdfFont regularFont = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);

                // Başlık bölümü
                var headerTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();

                // Logo - JSON'dan gelen path'i kullan
                string logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storeInfo.InvoiceInfo.logoPath.TrimStart('/'));
                if (System.IO.File.Exists(logoPath))
                {
                    ImageData logoData = ImageDataFactory.Create(logoPath);
                    Image logo = new Image(logoData).SetWidth(30).SetAutoScaleHeight(true);
                    headerTable.AddCell(new Cell().Add(logo).SetBorder(Border.NO_BORDER));
                }
                else
                {
                    headerTable.AddCell(new Cell().Add(new Paragraph("")).SetBorder(Border.NO_BORDER));
                }

                // Şirket bilgileri - JSON'dan gelen bilgileri kullan
                var companyInfo = new Paragraph()
                    .Add(new Text(storeInfo.InvoiceInfo.storeName).SetFont(boldFont).SetFontSize(16))
                    .Add("\n")
                    .Add(new Text(storeInfo.InvoiceInfo.cityCountry).SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text(storeInfo.InvoiceInfo.contactInfo.tel).SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text(storeInfo.InvoiceInfo.contactInfo.email).SetFont(regularFont).SetFontSize(10));

                headerTable.AddCell(new Cell().Add(companyInfo)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(Border.NO_BORDER));

                document.Add(headerTable);

                // Fatura başlığı
                document.Add(new Paragraph("FATURA")
                    .SetFont(boldFont)
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(20)
                    .SetMarginBottom(20));

                // Müşteri ve fatura bilgileri
                var infoTable = new Table(new float[] { 1, 1 }).UseAllAvailableWidth();

                // Müşteri bilgileri
                var customerInfo = new Paragraph()
                    .Add(new Text("MÜŞTERİ BİLGİLERİ").SetFont(boldFont).SetFontSize(12))
                    .Add("\n")
                    .Add(new Text(order.ShippingAddress.NameSurname).SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text(order.ShippingAddress.AcikAdres).SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text(order.ShippingAddress.Telefon).SetFont(regularFont).SetFontSize(10));

                infoTable.AddCell(new Cell().Add(customerInfo).SetBorder(Border.NO_BORDER));

                // Fatura bilgileri
                var invoiceInfo = new Paragraph()
                    .Add(new Text("FATURA BİLGİLERİ").SetFont(boldFont).SetFontSize(12))
                    .Add("\n")
                    .Add(new Text($"Fatura No: INV-{orderCode}").SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text($"Tarih: {DateTime.Now:dd.MM.yyyy}").SetFont(regularFont).SetFontSize(10))
                    .Add("\n")
                    .Add(new Text($"Ödeme: {order.PaymentMethod.PaymentMethodType}").SetFont(regularFont).SetFontSize(10));

                infoTable.AddCell(new Cell().Add(invoiceInfo)
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(Border.NO_BORDER));

                document.Add(infoTable);

                // Ürünler tablosu
                var productTable = new Table(new float[] { 3, 1, 1, 1 })
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                // Tablo başlıkları
                productTable.AddHeaderCell(CreateHeaderCell("Ürün", boldFont));
                productTable.AddHeaderCell(CreateHeaderCell("Adet", boldFont));
                productTable.AddHeaderCell(CreateHeaderCell("Birim Fiyat", boldFont));
                productTable.AddHeaderCell(CreateHeaderCell("Toplam", boldFont));

                // Ürünler
                foreach (var item in order.OrderItems)
                {
                    productTable.AddCell(CreateProductCell(item.ProductVariant?.Product?.Name ?? "Bilinmiyor", regularFont));
                    productTable.AddCell(CreateProductCell(item.Quantity.ToString(), regularFont));
                    productTable.AddCell(CreateProductCell($"₺{item.Price:F2}", regularFont));
                    productTable.AddCell(CreateProductCell($"₺{item.TotalPrice:F2}", regularFont));
                }

                document.Add(productTable);

                // Toplam bölümü
                var totalTable = new Table(new float[] { 4, 1 })
                    .UseAllAvailableWidth()
                    .SetMarginTop(20);

                totalTable.AddCell(new Cell(1, 1).Add(new Paragraph("")).SetBorder(Border.NO_BORDER));
                totalTable.AddCell(new Cell(1, 1)
                    .Add(new Paragraph($"Toplam: ₺{order.TotalPrice:F2}")
                        .SetFont(boldFont).SetFontSize(12))
                    .SetTextAlignment(TextAlignment.RIGHT)
                    .SetBorder(Border.NO_BORDER));

                document.Add(totalTable);

                // Footer
                document.Add(new Paragraph()
                    .Add(new Text("Teşekkür ederiz!").SetFont(boldFont).SetFontSize(10))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(30));

                document.Add(new Paragraph()
                    .Add(new Text($"Siparişiniz için teşekkür ederiz. Sorularınız için {storeInfo.InvoiceInfo.contactInfo.email} adresinden bize ulaşabilirsiniz.")
                        .SetFont(regularFont).SetFontSize(8))
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(10));
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            Response.Headers.Add("Content-Disposition", "inline; filename=Invoice_" + orderCode + ".pdf");
            return File(fileBytes, "application/pdf");
        }

        // Yardımcı metodlar
        private Cell CreateHeaderCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);
        }

        private Cell CreateProductCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text).SetFont(font).SetFontSize(9))
                .SetPadding(5)
                .SetBorderBottom(new SolidBorder(DeviceRgb.BLACK, 0.5f));
        }

        // JSON deserialization için model
        [HttpGet]
        public async Task<IActionResult> GetInvoiceInfo()
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // Debugging: JSON'u kontrol edelim
                Console.WriteLine(jsonObj.ToString());

                // InvoiceInfo'yu JSON'dan alıyoruz
                var invoiceInfo = jsonObj["InvoiceInfo"];

                // Eğer InvoiceInfo null veya boşsa hata döndürelim
                if (invoiceInfo == null || !invoiceInfo.HasValues)
                {
                    return BadRequest("InvoiceInfo verisi bulunamadı veya eksik.");
                }

                // InvoiceInfo'dan ilgili bilgileri alıyoruz
                var logoPath = invoiceInfo["logoPath"]?.ToString();
                var storeName = invoiceInfo["storeName"]?.ToString();
                var cityCountry = invoiceInfo["cityCountry"]?.ToString();
                var contactInfo = invoiceInfo["contactInfo"];

                if (contactInfo != null)
                {
                    var tel = contactInfo["tel"]?.ToString();
                    var email = contactInfo["email"]?.ToString();

                    // Veriyi kontrol etmek için loglayalım
                    Console.WriteLine($"Tel: {tel}, Email: {email}");
                }

                // Döndürülecek JSON verisini oluşturuyoruz
                var result = new
                {
                    LogoPath = logoPath,
                    StoreName = storeName,
                    CityCountry = cityCountry,
                    ContactInfo = new
                    {
                        Tel = contactInfo["tel"]?.ToString(),
                        Email = contactInfo["email"]?.ToString()
                    }
                };

                return Ok(result);  // İlgili veriyi geri döndürüyoruz
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }
    }
}