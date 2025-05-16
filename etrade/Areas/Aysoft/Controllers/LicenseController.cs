using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Aysoft.Services;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Aysoft.Controllers
{
    [Area("Aysoft")]
    [Authorize(AuthenticationSchemes = "CustomerCookie")]
    public class LicenseController : Controller
    {
        private readonly TenantContext _context;
        private readonly UserManager<TenantUser> _userManager;
        private readonly PasswordResetService _passwordResetService;
        private readonly LicenseSettingsService _licenseSettingsService;



        public LicenseController(TenantContext context, UserManager<TenantUser> userManager, PasswordResetService passwordResetService, LicenseSettingsService licenseSettingsService)
        {
            _userManager = userManager;
            _passwordResetService = passwordResetService;
            _context = context;
            _licenseSettingsService = licenseSettingsService;

        }
        public async Task<IActionResult> Status()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToAction("Login", "Account");

            // TenantCustomer bilgisi alınıyor
            var tenantCustomer = await _context.TenantCustomers
                .FirstOrDefaultAsync(x => x.UserId == identityUser.Id);

            if (tenantCustomer == null)
            {
                ViewBag.Message = "Kullanıcıya ait müşteri kaydı bulunamadı.";
                return View();
            }

            // İlgili kullanıcıya ait en güncel (ve silinmemiş) lisans kaydı
            var license = await _context.Licenses
                .Include(x => x.Payments)
                .Include(x => x.TenantStores)
                .Include(x => x.CancellationRequests)
                .Include(x => x.FreezePayments)
                .FirstOrDefaultAsync(x => x.CustomerId == tenantCustomer.Id);

            if (license == null)
            {
                ViewBag.Message = "Henüz tanımlı bir lisansınız bulunmamaktadır.";
                return View();
            }

            return View(license);
        }
        [HttpPost]
        public async Task<IActionResult> ResetCustomerPassword(int tenantCustomerId, string newPassword)
        {
            var success = await _passwordResetService.ResetUserPasswordAsync(tenantCustomerId, "customer@customer.com", newPassword);
            if (success) return Ok("Customer şifresi başarıyla güncellendi.");
            return BadRequest("Şifre güncellenemedi.");
        }

        [HttpPost]
        public async Task<IActionResult> ResetEditorPassword(int tenantCustomerId, string newPassword)
        {
            var success = await _passwordResetService.ResetUserPasswordAsync(tenantCustomerId, "editor@editor.com", newPassword);
            if (success) return Ok("Editor şifresi başarıyla güncellendi.");
            return BadRequest("Şifre güncellenemedi.");
        }
        [HttpPost]
        public async Task<IActionResult> ChangeAdminPassword(int tenantCustomerId, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifreler uyuşmuyor.";
                return RedirectToAction("YourViewName"); // formun bulunduğu sayfa
            }

            var success = await _passwordResetService.ResetUserPasswordAsync(tenantCustomerId, "admin@admin.com", newPassword);
            if (success)
            {
                TempData["Success"] = "Admin şifresi başarıyla güncellendi.";
            }
            else
            {
                TempData["Error"] = "Şifre güncellenemedi.";
            }

            return RedirectToAction("Status"); // formun bulunduğu sayfa
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDemoLicense(int duration)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // 1. TenantCustomer kaydını bul
            var tenantCustomer = await _context.TenantCustomers
                .FirstOrDefaultAsync(x => x.UserId == user.Id);
            if (tenantCustomer == null) return NotFound("Tenant müşteri bulunamadı.");

            // 2. Lisans kaydını al
            var license = await _context.Licenses
                .FirstOrDefaultAsync(x => x.CustomerId == tenantCustomer.Id);

            if (license == null)
            {
                license = new License
                {
                    CustomerId = tenantCustomer.Id
                };
                _context.Licenses.Add(license);
            }

            // 3. Lisans bilgilerini güncelle
            var startDate = DateTime.Now;
            var endDate = startDate.AddMonths(duration);

            license.StartDate = startDate;
            license.EndDate = endDate;
            license.DurationInMonths = duration;
            license.LicenseType = "Full"; // Enum değilse string

            // 4. Ödeme kayıtlarını oluştur
            var payments = new List<LicensePayment>();
            var licenseStart = startDate.AddDays(7); // 7 gün ücretsiz
            var shippingSettings = _licenseSettingsService.GetShippingSettings();

            for (int i = 0; i < duration; i++)
            {
                var start = licenseStart.AddMonths(i);
                var end = start.AddMonths(1).AddDays(-1);

                decimal basePrice = (i == 0) ? shippingSettings.StartLicense : shippingSettings.License;
                decimal kdvAmount = basePrice * shippingSettings.KDV;
                decimal priceWithKdv = basePrice + kdvAmount;

                var payment = new LicensePayment
                {
                    License = license, // alternatif olarak LicenseId = license.Id kullanabilirsin
                    StartPeriod = start,
                    EndPeriod = end,
                    Price = priceWithKdv,
                    IsPaid = false
                };

                payments.Add(payment);
            }

            _context.LicensePayments.AddRange(payments);
            await _context.SaveChangesAsync();

            return RedirectToAction("Status");
        }
        public IActionResult Frozen()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CheckCode(string code, int months)
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(x => x.FrozenCode == code);

            if (license == null)
            {
                TempData["Message"] = "Geçersiz kod.";
                return RedirectToAction("Frozen");
            }

            if (!license.IsFrozen)
            {
                TempData["Message"] = "Bu lisans zaten aktif.";
                return RedirectToAction("Frozen");
            }

            if (license.FreezeDate == null)
            {
                TempData["Message"] = "Dondurulma tarihi bulunamadı.";
                return RedirectToAction("Frozen");
            }

            // 6 ay geçmiş mi kontrolü
            var sixMonthsLater = license.FreezeDate.Value.AddMonths(6);
            if (DateTime.Now < sixMonthsLater)
            {
                TempData["Message"] = $"Lisansın tekrar aktif olması için {sixMonthsLater:dd.MM.yyyy} tarihine kadar beklemelisiniz.";
                return RedirectToAction("Frozen");
            }

            // Lisansı yeniden aktif hale getir
            license.IsFrozen = false;
            license.ActiveDate = DateTime.Now;
            license.DurationInMonths = months;

            var shippingSettings = _licenseSettingsService.GetShippingSettings();
            var payments = new List<LicensePayment>();
            var licenseStart = DateTime.UtcNow;

            for (int i = 0; i < months; i++)
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

            _context.LicensePayments.AddRange(payments);
            await _context.SaveChangesAsync();

            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Lisans başarıyla aktif hale getirildi ve yeni ödeme planı oluşturuldu.";
            return RedirectToAction("KodGir");
        }
        [AllowAnonymous]
        public JsonResult GetShippingSettings()
        {
            var shippingSettings = _licenseSettingsService.GetShippingSettings();
            var startLicense = shippingSettings.StartLicense;
            var license = shippingSettings.License;

            var result = new
            {
                StartLicense = startLicense,
                License = license
            };

            return Json(result);  // JSON olarak döndürüyoruz
        }
        [AllowAnonymous]
        public IActionResult Pricing()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreLicense(int id)
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null)
                return NotFound();

            var cancelRequest = await _context.CancellationRequests
                .FirstOrDefaultAsync(c => c.LicenseId == id);

            if (cancelRequest != null)
                _context.CancellationRequests.Remove(cancelRequest);

            license.IsDeleted = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Lisans erişime tekrar açıldı.";
            return RedirectToAction(nameof(Status), new { id = license.Id });
        }
        [HttpPost]
        public async Task<IActionResult> ExtendLicense(int licenseId, int extensionMonths)
        {
            var license = await _context.Licenses.FindAsync(licenseId);
            if (license == null)
                return NotFound();

            var shippingSettings = _licenseSettingsService.GetShippingSettings();

            // Yeni dönem başlama tarihi: mevcut bitiş tarihinin ertesi günü ya da bugün
            var licenseStart = license.EndDate > DateTime.UtcNow
                ? license.EndDate.AddDays(1)
                : DateTime.UtcNow;

            var payments = new List<LicensePayment>();

            for (int i = 0; i < extensionMonths; i++)
            {
                var start = licenseStart.AddMonths(i);
                var end = start.AddMonths(1).AddDays(-1);

                // Tüm ödemeler normal lisans ücretinden yapılacak
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

                payments.Add(payment);
            }

            // Lisansın bitiş tarihini güncelle
            license.EndDate = payments.Last().EndPeriod;
            _context.Licenses.Update(license);

            // Ödeme kayıtlarını ekle
            await _context.LicensePayments.AddRangeAsync(payments);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{extensionMonths} aylık lisans uzatıldı. {payments.Count} yeni ödeme kaydı eklendi.";
            return RedirectToAction("Status");
        }

    }
}