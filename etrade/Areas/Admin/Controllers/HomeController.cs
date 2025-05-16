using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using etrade.Data.Concrete;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Iyzipay.Model;
using System.Globalization;
using Iyzipay.Request;
using Iyzipay;
using iText.Layout.Element;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.IO.Font.Constants;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Draw;
using etrade.Areas.Tenant.Services;
using etrade.Entity;

namespace etrade.Controllers;
[Area("Admin")]
[Authorize(Roles = "admin,editor")]
[Authorize(AuthenticationSchemes = "AdminCookie")]

public class HomeController : Controller
{

    private readonly ILogger<HomeController> _logger;
    private readonly EtradeContext _context;
    private readonly TenantContext _tenantContext;
    private readonly LicenseSettingsService _licenseSettingsService;


    public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor, TenantContext tenantContext, LicenseSettingsService licenseSettingsService)
    {
        _logger = logger;
        _tenantContext = tenantContext;
        _licenseSettingsService = licenseSettingsService;

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


    public class PaymentRequest
    {
        public int LicensePaymentId { get; set; }
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; }
        public string ExpireMonth { get; set; }
        public string ExpireYear { get; set; }
        public string Cvc { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public async Task<IActionResult> CompleteLicensePayment([FromBody] PaymentRequest paymentRequest)
    {
        try
        {
            if (paymentRequest == null)
            {
                return BadRequest(new { isSuccess = false, errorMessage = "Veri eksik: paymentRequest null" });
            }

            // Ödeme işlemini gerçekleştir
            var result = await CompleteLicensePaymentAsync(
                paymentRequest.LicensePaymentId,
                paymentRequest.CardHolderName,
                paymentRequest.CardNumber,
                paymentRequest.ExpireMonth,
                paymentRequest.ExpireYear,
                paymentRequest.Cvc,
                paymentRequest.TotalPrice
            );

            if (result.IsSuccess)
            {
                // Ödeme başarılı olduğunda LicensePayment tablosunda güncelleme yap
                var licensePayment = await _tenantContext.LicensePayments
                    .FirstOrDefaultAsync(lp => lp.Id == paymentRequest.LicensePaymentId);

                if (licensePayment != null)
                {
                    licensePayment.IsPaid = true;
                    licensePayment.PaymentToken = result.PaymentToken;
                    await _tenantContext.SaveChangesAsync();
                }

                return Ok(new { isSuccess = true, paymentToken = result.PaymentToken });
            }
            else
            {
                return BadRequest(new { isSuccess = false, errorMessage = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { isSuccess = false, errorMessage = "Genel hata oluştu: " + ex.Message });
        }
    }

    private async Task<(bool IsSuccess, string ErrorMessage, string PaymentToken)> CompleteLicensePaymentAsync(
     int licensePaymentId, string cardHolderName, string cardNumber, string expireMonth, string expireYear, string cvc, decimal totalPrice)
    {
        try
        {
            Iyzipay.Options options = new Iyzipay.Options
            {
                ApiKey = "sandbox-F6PR5Gda82x2lahdseMEcnpjAyDqUQSP",
                SecretKey = "sandbox-ctcF7lH8ygz2sUn7kEhjTiCstD0U9XvA",
                BaseUrl = "https://sandbox-api.iyzipay.com"
            };

            CreatePaymentRequest request = new CreatePaymentRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                Price = totalPrice.ToString("F2", CultureInfo.InvariantCulture),
                PaidPrice = totalPrice.ToString("F2", CultureInfo.InvariantCulture),
                Currency = Currency.TRY.ToString(),
                Installment = 1,
                PaymentChannel = PaymentChannel.WEB.ToString(),
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = "https://seninsite.com/payment-callback",
                PaymentCard = new PaymentCard
                {
                    CardHolderName = cardHolderName,
                    CardNumber = cardNumber,
                    ExpireMonth = expireMonth,
                    ExpireYear = expireYear,
                    Cvc = cvc,
                    RegisterCard = 0
                },
                Buyer = new Buyer
                {
                    Id = "BY789",
                    Name = "Ad",
                    Surname = "Soyad",
                    GsmNumber = "+905350000000",
                    Email = "email@email.com",
                    IdentityNumber = "74300864791",
                    LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    RegistrationAddress = "Adres",
                    Ip = "85.34.78.112",
                    City = "İstanbul",
                    Country = "Turkey",
                    ZipCode = "34000"
                },
                ShippingAddress = new Iyzipay.Model.Address
                {
                    ContactName = "Ad Soyad",
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Adres açıklaması",
                    ZipCode = "34000"
                },
                BillingAddress = new Iyzipay.Model.Address
                {
                    ContactName = "Ad Soyad",
                    City = "İstanbul",
                    Country = "Turkey",
                    Description = "Fatura adresi açıklaması",
                    ZipCode = "34000"
                },
                BasketItems = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = licensePaymentId.ToString(),
                    Name = "Lisans Ücreti",
                    Category1 = "Lisans",
                    ItemType = BasketItemType.VIRTUAL.ToString(),
                    Price = totalPrice.ToString("F2", CultureInfo.InvariantCulture)
                }
            }
            };

            Payment payment;
            try
            {
                payment = await Payment.Create(request, options);
            }
            catch (Exception ex)
            {
                return (false, $"Ödeme işlemi başlatılırken hata oluştu: {ex.Message}", null);
            }

            if (payment == null)
            {
                return (false, "Ödeme yanıtı alınamadı. İyzico yanıtı: " + JsonConvert.SerializeObject(request), null);
            }

            if (payment.Status == "success")
            {
                return (true, null, payment.PaymentId); // ödeme başarılı
            }
            else
            {
                return (false, payment.ErrorMessage, null); // ödeme başarısız
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
    public async Task<IActionResult> GetLicensePayments()
    {
        var domain = Request.Host.Host;
        var tenantStore = await _tenantContext.TenantStores
            .FirstOrDefaultAsync(ts => ts.Domain == domain);

        if (tenantStore == null)
        {
            return NotFound("Domain ile ilişkilendirilmiş bir mağaza bulunamadı.");
        }

        var tenantId = tenantStore.LicenseId;

        // License bilgilerini al
        var license = await _tenantContext.Licenses
            .Include(l => l.TenantCustomer)
            .FirstOrDefaultAsync(l => l.Id == tenantId);

        if (license == null)
        {
            return NotFound("License bilgisi bulunamadı.");
        }

        // Ayarları çek
        var shippingSettings = _licenseSettingsService.GetShippingSettings();
        var kdvRate = shippingSettings.KDV;

        // LicensePayment kayıtlarını al
        var payments = await _tenantContext.LicensePayments
            .Where(lp => lp.License.Id == tenantId)
            .ToListAsync();

        // Ödeme gösterimleri
        var paymentsDto = payments.Select(lp =>
        {
            decimal netPrice = lp.Price / (1 + kdvRate); // 18000 / 1.2 = 15000
            string priceDisplay = $"{netPrice:N0} + KDV";

            return new
            {
                lp.Id,
                lp.StartPeriod,
                lp.EndPeriod,
                lp.Price, // Brüt fiyat (KDV dahil)
                PriceDisplay = priceDisplay,
                lp.IsPaid
            };
        }).ToList();

        var totalDebt = payments.Sum(p => p.Price);
        var paidDebt = payments.Where(p => p.IsPaid).Sum(p => p.Price);
        var remainingDebt = payments.Where(p => !p.IsPaid).Sum(p => p.Price);

        // Tek seferlik Customer bilgileri
        var customer = new
        {
            Name = license.TenantCustomer.FullName,
            Email = license.TenantCustomer.Email,
            Store = license.TenantCustomer.CompanyName
        };

        // Tek seferlik License bilgileri
        var licenseInfo = new
        {
            LicenseId = license.Id,
            StartDate = license.StartDate,
            EndDate = license.EndDate,
            RemainingDays = (license.EndDate - DateTime.Now).Days
        };

        var result = new
        {
            License = licenseInfo,
            Customer = customer,
            Payments = paymentsDto,
            TotalDebt = totalDebt,
            PaidDebt = paidDebt,
            RemainingDebt = remainingDebt
        };

        return Ok(result);
    }
    public async Task<IActionResult> GetCancellationRequestStatus()
    {
        // Domain bilgisini al
        var domain = Request.Host.Host;

        // Domain ile ilişkili TenantStore'u bul
        var tenantStore = await _tenantContext.TenantStores
            .FirstOrDefaultAsync(ts => ts.Domain == domain);

        if (tenantStore == null)
        {
            return NotFound("Domain ile ilişkilendirilmiş bir mağaza bulunamadı.");
        }

        var licenseId = tenantStore.LicenseId;

        // LicenseId ile ilişkili CancellationRequest'i bul
        var cancellationRequest = await _tenantContext.CancellationRequests
            .FirstOrDefaultAsync(cr => cr.LicenseId == licenseId);

        if (cancellationRequest == null)
        {
            return NotFound("Bu LicenseId'ye ait bir iptal talebi bulunamadı.");
        }

        // IsCompleted ve IsApproved değerlerini döndür
        var result = new
        {
            IsCompleted = cancellationRequest.IsCompleted,
            IsApproved = cancellationRequest.IsApproved,
            HasPendingRequest = true
        };

        return Ok(result);
    }
    public class LicensePaymentViewModel
    {
        public int Id { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public decimal Price { get; set; }
        public bool IsPaid { get; set; }
    }

    public async Task<IActionResult> Index()
    {
        // "Taslak" ve "Ödenmedi" durumundaki siparişleri alıyoruz
        var ordersToDelete = await _context.Orders
            .Where(o => o.OrderStatus == "Taslak" && o.PaymentStatus == "Ödenmedi")
            .ToListAsync();  // "Taslak" ve "Ödenmedi" durumundaki siparişleri buluyoruz

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

            await _context.SaveChangesAsync(); // Veritabanındaki değişiklikleri kaydet
        }

        // Güncellenmiş tüm siparişleri alıp view'a gönderiyoruz
        return View(); // Admin sayfasındaki tüm siparişleri gösteriyoruz
    }
    // Örnek API Controller
    public async Task<IActionResult> GetStoreLogos()
    {
        var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync(ss => ss.Id == 1);
        if (storeSetting == null)
        {
            return NotFound("Logo ayarları bulunamadı.");
        }

        return Ok(new
        {
            logoDarkPath = storeSetting.LogoPath,
            logoLightPath = storeSetting.LogoWhitePath
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    [HttpGet]
    public async Task<IActionResult> ViewInvoice(int paymentId)
    {
        var payment = await _tenantContext.LicensePayments
            .Include(lp => lp.License)
            .ThenInclude(l => l.TenantCustomer)
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        // Ödeme yapılmamışsa (IsPaid == false), fatura oluşturulmasın
        if (payment == null || !payment.IsPaid)
        {
            return NotFound("Ödeme bulunamadı veya ödeme yapılmamış.");
        }

        var customer = payment.License.TenantCustomer;

        using (var memoryStream = new MemoryStream())
        {
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4);

            // Font ayarları
            string fontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arial.ttf");
            string boldFontPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "ARIALBD.ttf");

            PdfFont font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
            PdfFont boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H);

            // Logo ve şirket bilgileri
            var headerTable = new Table(new float[] { 1, 2 })
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Logo kısmı (burada metin kullanıyoruz ama gerçekte bir resim ekleyebilirsiniz)
            var logoCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("AYSOFT").SetFont(boldFont).SetFontSize(20).SetFontColor(new DeviceRgb(0, 51, 102)));

            // Şirket iletişim bilgileri
            var companyInfoCell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("AYSOFT Yazılım Çözümleri").SetFont(boldFont).SetFontSize(12))
                .Add(new Paragraph("İş Merkezi, Konyaaltı").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("Antalya, Türkiye").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("Vergi No: 1234567890").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("support@aysoft.com").SetFont(font).SetFontSize(10))
                .Add(new Paragraph("+90 242 123 45 67").SetFont(font).SetFontSize(10));

            headerTable.AddCell(logoCell);
            headerTable.AddCell(companyInfoCell);
            document.Add(headerTable);

            // Fatura başlık bölümü
            var invoiceHeader = new Table(new float[] { 2, 1 })
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Fatura bilgileri
            var invoiceInfo = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(10)
                .SetBackgroundColor(new DeviceRgb(0, 51, 102))
                .Add(new Paragraph("FATURA").SetFont(boldFont).SetFontSize(18).SetFontColor(DeviceRgb.WHITE))
                .Add(new Paragraph($"Fatura No: FTR-{payment.Id}").SetFont(font).SetFontSize(10).SetFontColor(DeviceRgb.WHITE))
                .Add(new Paragraph($"Fatura Tarihi: {DateTime.Now:dd.MM.yyyy}").SetFont(font).SetFontSize(10).SetFontColor(DeviceRgb.WHITE));

            // Müşteri bilgileri (daha profesyonel bir sunum)
            var customerInfo = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetPadding(10)
                .SetBackgroundColor(new DeviceRgb(240, 240, 240))
                .Add(new Paragraph("FATURA KESİLEN FİRMA VE YETKİLİ BİLGİSİ").SetFont(boldFont).SetFontSize(12))
                .Add(new Paragraph(customer.FullName).SetFont(boldFont).SetFontSize(11).SetMarginTop(5))
                .Add(new Paragraph($"Vergi No: {customer.TaxNumber ?? "Belirtilmemiş"}").SetFont(font).SetFontSize(10))
                .Add(new Paragraph(customer.Address ?? "Adres bilgisi belirtilmemiş").SetFont(font).SetFontSize(10))
                .Add(new Paragraph($"Tel: {customer.Phone}").SetFont(font).SetFontSize(10))
                .Add(new Paragraph($"E-posta: {customer.Email}").SetFont(font).SetFontSize(10));

            invoiceHeader.AddCell(customerInfo);
            invoiceHeader.AddCell(invoiceInfo);
            document.Add(invoiceHeader);

            // Fatura detayları
            document.Add(new Paragraph("FATURA KALEMLERİ").SetFont(boldFont).SetFontSize(12).SetMarginBottom(10));

            // Tablo başlığı
            var itemTable = new Table(new float[] { 1, 3, 2, 2, 2 })
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            var shippingSettings = _licenseSettingsService.GetShippingSettings();
            decimal kdvOrani = shippingSettings.KDV;
            decimal netPrice = payment.Price / (1 + kdvOrani);

            itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Sıra").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Açıklama").SetFont(boldFont)));
            itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Dönem").SetFont(boldFont).SetTextAlignment(TextAlignment.CENTER)));
            itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Birim Fiyat").SetFont(boldFont).SetTextAlignment(TextAlignment.RIGHT)));
            itemTable.AddHeaderCell(new Cell().Add(new Paragraph("Tutar").SetFont(boldFont).SetTextAlignment(TextAlignment.RIGHT)));

            itemTable.AddCell(new Cell().Add(new Paragraph("1").SetFont(font).SetTextAlignment(TextAlignment.CENTER)));
            itemTable.AddCell(new Cell().Add(new Paragraph("Yazılım Lisans Ücreti").SetFont(font)));
            itemTable.AddCell(new Cell().Add(new Paragraph($"{payment.StartPeriod:dd.MM.yyyy}\n{payment.EndPeriod:dd.MM.yyyy}").SetFont(font).SetTextAlignment(TextAlignment.CENTER)));
            itemTable.AddCell(new Cell().Add(new Paragraph($"{netPrice:N2} ₺").SetFont(font).SetTextAlignment(TextAlignment.RIGHT))); // KDV'siz fiyat
            itemTable.AddCell(new Cell().Add(new Paragraph($"{payment.Price:N2} ₺").SetFont(font).SetTextAlignment(TextAlignment.RIGHT))); // KDV dahil

            document.Add(itemTable);

            // Toplam bölümü
            var totalTable = new Table(new float[] { 3, 2 })
                .UseAllAvailableWidth()
                .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                .SetWidth(250);

            decimal kdv = payment.Price - netPrice;           // 18000 - 15254.24 = 2745.76

            totalTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph("Ara Toplam:").SetFont(font)));
            totalTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"{netPrice:N2} ₺").SetFont(font).SetTextAlignment(TextAlignment.RIGHT)));

            totalTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"KDV (%{kdvOrani * 100:N0}):").SetFont(font)));
            totalTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                .Add(new Paragraph($"{kdv:N2} ₺").SetFont(font).SetTextAlignment(TextAlignment.RIGHT)));

            totalTable.AddCell(new Cell()
                .SetBorderTop(new SolidBorder(1))
                .SetBorderBottom(new SolidBorder(1))
                .Add(new Paragraph("GENEL TOPLAM:").SetFont(boldFont)));
            totalTable.AddCell(new Cell()
                .SetBorderTop(new SolidBorder(1))
                .SetBorderBottom(new SolidBorder(1))
                .Add(new Paragraph($"{payment.Price:N2} ₺").SetFont(boldFont).SetTextAlignment(TextAlignment.RIGHT)));

            document.Add(totalTable);

            // Footer
            var footer = new Div()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20)
                .Add(new Paragraph("AYSOFT Yazılım Çözümleri").SetFont(boldFont).SetFontSize(10))
                .Add(new Paragraph("Bu belge elektronik faturanızdır, kaşe ve imza gerektirmez").SetFont(font).SetFontSize(8).SetFontColor(new DeviceRgb(100, 100, 100)))
                .Add(new Paragraph("www.aysoft.com | info@aysoft.com | +90 242 123 45 67").SetFont(font).SetFontSize(8));

            document.Add(footer);

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            Response.Headers.Add("Content-Disposition", "inline; filename=Fatura-AYS-" + payment.Id + ".pdf");
            return File(pdfBytes, "application/pdf");
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCancellationRequest([FromBody] CancellationRequestViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest("Geçersiz istek.");

        var license = await _tenantContext.Licenses
            .Include(l => l.CancellationRequests)  // İptal talepleri
            .FirstOrDefaultAsync(l => l.Id == model.LicenseId);

        if (license == null)
            return NotFound("Lisans bulunamadı.");

        // Daha önce tamamlanmamış bir iptal talebi varsa engelle
        bool hasPendingRequest = license.CancellationRequests.Any(cr => !cr.IsCompleted);
        if (hasPendingRequest)
            return BadRequest("Zaten bekleyen bir iptal talebiniz mevcut.");

        var today = DateTime.UtcNow.Date;

        // Geçmiş dönem ödenmemiş fatura var mı?
        var hasUnpaidPast = await _tenantContext.LicensePayments
            .Where(lp => lp.LicenseId == model.LicenseId && lp.EndPeriod < today && !lp.IsPaid)
            .AnyAsync();

        if (hasUnpaidPast)
        {
            return BadRequest("İptal talebi gönderilemez. Önce geçmiş dönem ödemelerinizi gerçekleştirmeniz gerekmektedir.");
        }

        // İçinde bulunduğumuz dönemi bul ve ödeme yapılmamışsa kontrol et
        var currentPayment = await _tenantContext.LicensePayments
            .Where(lp => lp.LicenseId == model.LicenseId && lp.StartPeriod <= today && lp.EndPeriod >= today)
            .FirstOrDefaultAsync();

        if (currentPayment != null && !currentPayment.IsPaid)
        {
            var daysSinceStart = (today - currentPayment.StartPeriod.Date).TotalDays;
            if (daysSinceStart >= 15)
            {
                return BadRequest("İçinde bulunduğunuz dönemin ödemesini gerçekleştirmeden iptal talebi gönderemezsiniz.");
            }
        }

        var request = new CancellationRequest
        {
            LicenseId = model.LicenseId,
            Reason = model.Reason,
            RequestDate = DateTime.UtcNow,
            IsApproved = false,
            IsCompleted = false
        };

        _tenantContext.CancellationRequests.Add(request);
        await _tenantContext.SaveChangesAsync();

        return Ok(new { success = true, message = "İptal talebiniz başarıyla oluşturuldu." });
    }
    public class CancellationRequestViewModel
    {
        public int LicenseId { get; set; }
        public string Reason { get; set; }
    }
    [HttpPost]
    public IActionResult Finalize(int licenseId)
    {
        // Lisansı veritabanından bul
        var license = _tenantContext.Licenses.FirstOrDefault(x => x.Id == licenseId);

        if (license == null)
        {
            return Json(new { success = false, message = "Lisans bulunamadı." });
        }

        // Zaten silinmiş mi kontrol et (opsiyonel)
        if (license.IsDeleted)
        {
            return Json(new { success = false, message = "Bu lisans zaten sonlandırılmış." });
        }

        // Lisansı işaretle
        license.IsDeleted = true;
        license.DeletionDate = DateTime.Now;

        // Değişiklikleri kaydet
        _tenantContext.SaveChanges();

        return Json(new { success = true });
    }
    public IActionResult Frozen(int id)
    {
        return View();
    }
    [HttpGet]
    public IActionResult GetFrozen(int id)
    {
        // FreezePayments tablosunda bu lisans için ödeme yapılmış mı?
        bool exists = _tenantContext.FreezePayments.Any(x => x.LicenseId == id);

        string freezeCode = null;

        if (exists)
        {
            // License kaydını getir
            var license = _tenantContext.Licenses.FirstOrDefault(x => x.Id == id);

            if (license != null)
            {
                freezeCode = license.FrozenCode;
            }
        }

        return Json(new { result = exists, freezeCode = freezeCode });
    }
    public async Task<IActionResult> CompleteFreezePayment([FromBody] PaymentRequest paymentRequest)
    {
        try
        {
            if (paymentRequest == null)
            {
                TempData["ErrorMessage"] = "Veri eksik: paymentRequest null";
                return RedirectToAction("Frozen"); // Ya da mevcut sayfanın adını yazın
            }

            // Ödeme işlemini gerçekleştir
            var result = await CompleteLicensePaymentAsync(
                paymentRequest.LicensePaymentId,
                paymentRequest.CardHolderName,
                paymentRequest.CardNumber,
                paymentRequest.ExpireMonth,
                paymentRequest.ExpireYear,
                paymentRequest.Cvc,
                paymentRequest.TotalPrice
            );

            if (result.IsSuccess)
            {
                // Benzersiz FreezeCode oluştur
                string freezeCode;
                do
                {
                    freezeCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); // Örn: 8 karakterli benzersiz kod
                }
                while (await _tenantContext.Licenses.AnyAsync(x => x.FrozenCode == freezeCode));

                // License'ı bul
                var license = await _tenantContext.Licenses.FirstOrDefaultAsync(x => x.Id == paymentRequest.LicensePaymentId);
                if (license == null)
                {
                    TempData["ErrorMessage"] = "İlgili lisans bulunamadı.";
                    return RedirectToAction("Frozen"); // Sayfada kalma
                }

                // FreezeCode'u kaydet
                license.FrozenCode = freezeCode;
                _tenantContext.Licenses.Update(license);

                // FreezePayments tablosuna yeni ödeme kaydı ekle
                var freezePayment = new FreezePayment
                {
                    IsPaid = true,
                    LicenseId = paymentRequest.LicensePaymentId,
                    TransactionId = result.PaymentToken,
                    Price = paymentRequest.TotalPrice,
                    PaymentDate = DateTime.UtcNow
                };
                _tenantContext.FreezePayments.Add(freezePayment);

                // Tüm değişiklikleri kaydet
                await _tenantContext.SaveChangesAsync();

                // TempData'ya başarılı ödeme mesajı ekleyelim
                TempData["SuccessMessage"] = "Ödeme başarıyla tamamlandı!";

                return RedirectToAction("Frozen"); // Kullanıcı aynı sayfada kalacak
            }
            else
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction("Frozen"); // Sayfada kalma
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Genel hata oluştu: " + ex.Message;
            return RedirectToAction("Frozen"); // Sayfada kalma
        }
    }
    public async Task<IActionResult> FreezeLicense(int id)
    {
        var license = await _tenantContext.Licenses.FindAsync(id);
        if (license == null)
        {
            return NotFound(new { message = "Lisans bulunamadı." });
        }

        if (license.IsFrozen)
        {
            return BadRequest(new { message = "Lisans zaten dondurulmuş." });
        }

        // 6 ay kısıtlaması kontrolü
        if (license.ActiveDate != null)
        {
            var daysSinceActivated = (DateTime.UtcNow - license.ActiveDate.Value).TotalDays;
            if (daysSinceActivated < 180)
            {
                return BadRequest(new { message = "Lisans dondurma için son aktifleştirmeden sonra en az 6 ay geçmiş olmalı." });
            }
        }

        var now = DateTime.UtcNow;

        var payments = await _tenantContext.LicensePayments
            .Where(p => p.LicenseId == id)
            .ToListAsync();

        // 1. Geçmişe ait ödenmemiş borç kontrolü
        bool hasPastDebt = payments.Any(p => p.EndPeriod < now && !p.IsPaid);
        if (hasPastDebt)
        {
            return BadRequest(new { message = "Geçmişe ait ödenmemiş borcunuz var. Lisans dondurulamaz." });
        }

        // 2. İçinde bulunduğumuz dönemin 15 gün geçip geçmediği kontrolü
        var currentPeriod = payments
            .FirstOrDefault(p => p.StartPeriod <= now && now <= p.EndPeriod);

        if (currentPeriod != null && !currentPeriod.IsPaid)
        {
            var daysSincePeriodStart = (now - currentPeriod.StartPeriod).TotalDays;
            if (daysSincePeriodStart > 15)
            {
                return BadRequest(new { message = "İçinde bulunduğunuz dönemin ödeme süresi 15 günü geçti. Önce ödeme yapılmalı." });
            }
        }

        // Dondurma işlemi
        license.IsFrozen = true;
        license.FreezeDate = now;
        license.ActiveDate = null;

        // Gelecekteki ödemeleri sil
        var futurePayments = payments
            .Where(p => p.StartPeriod > now)
            .ToList();

        _tenantContext.LicensePayments.RemoveRange(futurePayments);

        _tenantContext.Licenses.Update(license);
        await _tenantContext.SaveChangesAsync();

        return Ok(new { result = true, message = "Lisans başarıyla donduruldu." });
    }
    public IActionResult GetSettings()
    {
        var settings = _licenseSettingsService.GetShippingSettings();

        var freezePrice = settings?.Freeze ?? 0m;

        return Json(new { freezePrice });
    }
}



