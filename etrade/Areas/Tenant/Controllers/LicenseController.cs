using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]


    public class LicenseController : Controller
    {
        private readonly ILogger<HealthController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;
        private readonly LicenseSettingsService _licenseSettingsService;

        private readonly UserManager<TenantUser> _userManager;
        private readonly RoleManager<TenantRole> _roleManager;


        // Controller için constructor (yapıcı metod)
        public LicenseController(ILogger<HealthController> logger, TenantContext tenantContext, TenantService tenantService, LicenseSettingsService licenseSettingsService, UserManager<TenantUser> userManager,
            RoleManager<TenantRole> roleManager)
        {
            _logger = logger;
            _licenseSettingsService = licenseSettingsService;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public IActionResult AddCustomer()
        {
            var cities = _tenantContext.Iller.ToList(); // Şehir bilgilerini alıyoruz
            ViewData["Cities"] = cities;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomer([FromBody] TenantCustomer customer)
        {
            if (customer == null)
            {
                return BadRequest(new { message = "Geçersiz müşteri verisi." });
            }

            // Şifre kontrolü frontend'de yapılmış olsa bile burada tekrar kontrol etmek önemlidir.
            if (string.IsNullOrWhiteSpace(customer.Password) || customer.Password != customer.ConfirmPassword)
            {
                return BadRequest(new { message = "Şifreler boş olamaz ve uyuşmalıdır." });
            }

            // Önce kullanıcıyı Identity üzerinden oluştur
            var user = new TenantUser
            {
                UserName = customer.UserName,
                Email = customer.Email,
                PhoneNumber = customer.Phone              
            };

            var result = await _userManager.CreateAsync(user, customer.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { message = "Kullanıcı oluşturulamadı.", errors });
            }

            // Role ekle (örneğin "Customer" rolü varsa)
            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                await _roleManager.CreateAsync(new TenantRole { Name = "Customer" });
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            // TenantCustomer tablosuna kayıt
            customer.UserId = user.Id; // Identity'den dönen kullanıcı ID'si
            _tenantContext.TenantCustomers.Add(customer);
            await _tenantContext.SaveChangesAsync();

            return Ok(new { message = "Müşteri başarıyla kaydedildi!" });
        }
        [HttpGet]
        public IActionResult AddLicense()
        {
            var usedStoreIds = _tenantContext.Licenses
                .SelectMany(l => l.TenantStores)
                .Select(ts => ts.Id)
                .ToList();

            ViewBag.Stores = _tenantContext.TenantStores
                .Where(s => !usedStoreIds.Contains(s.Id))
                .ToList();

            ViewBag.Customers = _tenantContext.TenantCustomers.ToList(); // <-- eksikti, eklendi

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddLicense([FromBody] License license)
        {
            if (license == null)
            {
                return BadRequest(new { message = "Geçersiz lisans verisi." });
            }

            try
            {
                // EndDate hesaplanıyor
                license.EndDate = license.StartDate.AddMonths(license.DurationInMonths);
                license.LicenseType = "Full";

                if (license.TenantStores != null && license.TenantStores.Any())
                {
                    var storeList = new List<TenantStore>();

                    foreach (var store in license.TenantStores)
                    {
                        var foundStore = await _tenantContext.TenantStores.FindAsync(store.Id);

                        if (foundStore == null)
                            continue;

                        // Bu mağaza daha önce lisanslanmış mı?
                        bool alreadyLicensed = await _tenantContext.Licenses
                            .AnyAsync(l => l.TenantStores.Any(ts => ts.Id == foundStore.Id));

                        if (alreadyLicensed)
                        {
                            return BadRequest(new
                            {
                                message = $"'{foundStore.StoreName}' mağazası zaten bir lisansa atanmış."
                            });
                        }

                        storeList.Add(foundStore);
                    }

                    license.TenantStores = storeList;
                }

                _tenantContext.Licenses.Add(license);
                await _tenantContext.SaveChangesAsync(); // Lisans ID'si oluşsun

                // Lisans Ödeme Kayıtları oluşturuluyor
                var payments = new List<LicensePayment>();
                var licenseStart = license.StartDate.AddDays(7); // 7 gün ücretsiz
                var shippingSettings = _licenseSettingsService.GetShippingSettings();


                for (int i = 0; i < license.DurationInMonths; i++)
                {
                    var start = licenseStart.AddMonths(i);
                    var end = start.AddMonths(1).AddDays(-1);

                    decimal basePrice = (i == 0) ? shippingSettings.StartLicense : shippingSettings.License;
                    decimal kdvAmount = basePrice * shippingSettings.KDV;
                    decimal priceWithKdv = basePrice + kdvAmount;

                    var payment = new LicensePayment
                    {
                        LicenseId = license.Id,
                        StartPeriod = start,
                        EndPeriod = end,
                        Price = priceWithKdv,
                        IsPaid = false
                    };

                    payments.Add(payment);
                }

                await _tenantContext.LicensePayments.AddRangeAsync(payments);
                await _tenantContext.SaveChangesAsync();

                return Ok(new { message = "Lisans ve ödeme kayıtları başarıyla kaydedildi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lisans kaydedilirken bir hata oluştu.",
                    error = ex.Message
                });
            }
        }
        public async Task<IActionResult> GetLicensePayments()
        {
            try
            {
                var payments = await _tenantContext.LicensePayments
                    .Include(lp => lp.License)
                    .Include(lp => lp.License.TenantCustomer)
                    .Include(lp => lp.License.TenantStores)
                    .Where(lp =>
                        lp.License.TenantStores.Any(ts => ts.Domain != null) &&
                        !lp.License.IsDeleted) // IsDeleted false olanları getiriyoruz
                    .Select(lp => new
                    {
                        lp.Id,
                        lp.StartPeriod,
                        lp.EndPeriod,
                        lp.Price,
                        lp.IsPaid,
                        LicenseId = lp.License.Id,
                        LicenseStartDate = lp.License.StartDate,
                        LicenseEndDate = lp.License.EndDate,
                        CustomerFullName = lp.License.TenantCustomer.FullName,
                        CustomerEmail = lp.License.TenantCustomer.Email,
                        CustomerPhone = lp.License.TenantCustomer.Phone,
                        CustomerCompanyName = lp.License.TenantCustomer.CompanyName,
                        DomainName = lp.License.TenantStores.FirstOrDefault().Domain
                    })
                    .ToListAsync();

                return Ok(new { isSuccess = true, payments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> GetLicenses()
        {
            try
            {
                var licenses = await _tenantContext.Licenses
                    .Include(l => l.TenantCustomer)
                    .Include(l => l.TenantStores)
                    .Where(l => l.TenantStores.Any() && !l.IsDeleted) // sadece en az 1 mağazası olan ve IsDeleted false olan lisanslar
                    .OrderByDescending(l => l.StartDate)
                    .ToListAsync();

                if (!licenses.Any())
                {
                    return NotFound("Aktif lisans bulunamadı.");
                }

                var result = licenses.Select(l => new
                {
                    l.Id,
                    TenantCustomerName = l.TenantCustomer?.FullName ?? "Bilgi Yok",
                    l.StartDate,
                    l.EndDate,
                    l.DurationInMonths,
                    StoreCount = l.TenantStores.Count // aktif mağaza sayısı
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Bir hata oluştu: {ex.Message}");
            }
        }
        public async Task<IActionResult> GetCustomersWithLicenses()
        {
            try
            {
                // Tüm müşterileri ve ilişkili lisanslarını getirecek sorgu
                var customers = await _tenantContext.TenantCustomers
                    .Include(c => c.Licenses)  // İlişkili lisansları dahil et
                    .Select(c => new
                    {
                        c.Id,
                        c.FullName,
                        c.Email,
                        c.Phone,
                        c.CompanyName,
                        c.TaxNumber,
                        c.TaxOffice,
                        c.Address,
                        c.City.Ad,  // City ilişkisini de alıyoruz
                        c.ZipCode,
                        Licenses = c.Licenses.Select(l => new
                        {
                            l.Id,
                            l.StartDate,
                            l.EndDate,
                            l.DurationInMonths,
                            l.IsDeleted,
                            l.DeletionDate
                        })
                    })
                    .ToListAsync();

                return Ok(new { isSuccess = true, customers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateDuration(int licenseId, int newDuration)
        {
            try
            {
                var license = await _tenantContext.Licenses
                    .Include(l => l.Payments)
                    .FirstOrDefaultAsync(l => l.Id == licenseId);

                if (license == null)
                {
                    return NotFound(new { success = false, message = "Lisans bulunamadı." });
                }

                int oldDuration = license.DurationInMonths;

                // Eğer yeni süre eski süreden küçükse, eksiltme işlemi yapılmıyor
                if (newDuration > oldDuration)
                {
                    // Süre artırılıyorsa yeni ödeme kayıtları ekle
                    int monthsToAdd = newDuration - oldDuration;

                    // En son ödeme kaydının bitiş tarihi
                    var lastPayment = license.Payments
                        .OrderByDescending(p => p.EndPeriod)
                        .FirstOrDefault();

                    DateTime nextStartDate = lastPayment != null
                        ? lastPayment.EndPeriod.AddDays(1)
                        : license.StartDate;

                    var newPayments = new List<LicensePayment>();

                    // Yeni ödeme kayıtları oluştur
                    for (int i = 0; i < monthsToAdd; i++)
                    {
                        var start = nextStartDate.AddMonths(i);
                        var end = start.AddMonths(1).AddDays(-1);
                        var shippingSettings = _licenseSettingsService.GetShippingSettings();

                        decimal basePrice = shippingSettings.License;
                        decimal kdvAmount = basePrice * shippingSettings.KDV;
                        decimal priceWithKdv = basePrice + kdvAmount;

                        var payment = new LicensePayment
                        {
                            LicenseId = license.Id,
                            StartPeriod = start,
                            EndPeriod = end,
                            Price = priceWithKdv,
                            IsPaid = false
                        };

                        newPayments.Add(payment);
                    }

                    // Yeni ödeme kayıtlarını veritabanına ekle
                    await _tenantContext.LicensePayments.AddRangeAsync(newPayments);

                    // Son ödeme tarihini güncelle
                    var lastPaymentAfterChanges = newPayments.LastOrDefault();
                    if (lastPaymentAfterChanges != null)
                    {
                        license.EndDate = lastPaymentAfterChanges.EndPeriod;
                    }
                }

                // Lisans süresini ve bitiş tarihini güncelle
                license.DurationInMonths = newDuration;

                // Değişiklikleri kaydet
                await _tenantContext.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }
        public IActionResult PaymentList()
        {
            return View();
        }

        public IActionResult LicenseList()
        {
            return View();
        }
        public IActionResult CustomerList()
        {
            return View();
        }
        public IActionResult LicensePrice()
        {
            return View();
        }
        public IActionResult GetSettings()
        {
            var settings = _licenseSettingsService.GetShippingSettings();
            if (settings == null)
            {
                settings = new LicenseSettingsService.ShippingSettings();
            }
            return Json(settings);
        }

        [HttpPost]
        public IActionResult UpdateSettings([FromBody] LicenseSettingsService.ShippingSettings model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _licenseSettingsService.UpdateShippingSettings(model);
            return Ok(new { success = true, message = "Ayarlar başarıyla güncellendi." });
        }
        [HttpGet]
        public async Task<IActionResult> GetFrozenLicenses()
        {
            var frozenLicenses = await _tenantContext.Licenses
                .Include(l => l.TenantCustomer)
                .Include(l => l.TenantStores)
                .Where(l => l.IsFrozen)
                .Select(l => new
                {
                    LicenseId = l.Id,
                    CustomerName = l.TenantCustomer.FullName,
                    StartDate = l.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = l.EndDate.ToString("yyyy-MM-dd"),
                    FreezeDate = l.FreezeDate,
                    LicenseType = l.LicenseType,
                    Stores = l.TenantStores.Select(s => new
                    {
                        StoreId = s.Id,
                        StoreName = s.StoreName
                    })
                })
                .ToListAsync();

            return Json(frozenLicenses);
        }
        public IActionResult Frozen()
        {
            return View();
        }

    }
}