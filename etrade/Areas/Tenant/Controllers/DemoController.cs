using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]

    public class DemoController : Controller
    {
        private readonly ILogger<DemoController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;
        private readonly LicenseSettingsService _licenseSettingsService;
        private readonly UserManager<TenantUser> _userManager;



        // Controller için constructor (yapıcı metod)
        public DemoController(ILogger<DemoController> logger, TenantContext tenantContext, TenantService tenantService, LicenseSettingsService licenseSettingsService, UserManager<TenantUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            _licenseSettingsService = licenseSettingsService;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
        }
        [HttpGet]
        public async Task<IActionResult> GetDemoRequests()
        {
            var demoRequests = await _tenantContext.DemoRequests
                .Include(d => d.TenantCustomer) // TenantCustomer ile ilişkiyi dahil et
                .Select(d => new
                {
                    d.Id,
                    d.RequestDate,
                    d.RequestNote,
                    d.RequestStatus,
                    d.DemoDays,
                    d.DemoStartDate,
                    d.DemoEndDate,
                    d.ApprovedByAdminId,
                    d.ApprovedDate,
                    d.IsActive,
                    CustomerFullName = d.TenantCustomer.FullName, // TenantCustomer'ın ismi
                    CustomerEmail = d.TenantCustomer.Email, // TenantCustomer'ın e-posta adresi
                    CustomerPhone = d.TenantCustomer.Phone
                })
                .ToListAsync();

            if (demoRequests == null || !demoRequests.Any())
            {
                return NotFound(new { message = "Demo talepleri bulunamadı." });
            }

            return Ok(demoRequests); // JSON formatında döndür
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var request = await _tenantContext.DemoRequests
                .Include(x => x.TenantCustomer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
                return NotFound();

            // İlgili müşteriye ait mağazaları getir
            var stores = await _tenantContext.TenantStores
                .Where(s => s.LicenseId == null)
                .ToListAsync();

            ViewBag.Stores = new SelectList(stores, "Id", "StoreName");

            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Detail(int id, string newStatus)
        {
            var request = await _tenantContext.DemoRequests
                .Include(x => x.TenantCustomer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
                return NotFound();

            request.RequestStatus = newStatus;

            if (newStatus == "Tamamlandı" && !request.IsActive)
            {
                var admin = await _userManager.GetUserAsync(User);

                request.DemoStartDate = DateTime.UtcNow;
                request.DemoEndDate = DateTime.UtcNow.AddDays(request.DemoDays);
                request.IsActive = true;
                request.ApprovedDate = DateTime.UtcNow;
                request.ApprovedByAdminId = admin?.Id;
            }

            await _tenantContext.SaveChangesAsync();

            TempData["Success"] = "Talep güncellendi.";
            return RedirectToAction("Detail", new { id });
        }
        [HttpPost]
        public async Task<IActionResult> CreateLicense(int id, int storeId, DateTime startDate)
        {
            var request = await _tenantContext.DemoRequests
                .Include(x => x.TenantCustomer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (request == null)
                return NotFound();

            // Mağaza seçimi yapılmış mı?
            var store = await _tenantContext.TenantStores.FirstOrDefaultAsync(s => s.Id == storeId);
            if (store == null)
                return NotFound("Mağaza bulunamadı.");

            // Talep tamamlandıysa ve daha önce aktif değilse
            if (request.RequestStatus == "İşleme Alındı" && request.IsActive)
            {
                // Admin bilgilerini al
                var admin = await _userManager.GetUserAsync(User);

                // Demo lisansı oluştur
                var license = new License
                {
                    LicenseType = "Demo",
                    StartDate = startDate,  // Kullanıcının seçtiği başlangıç tarihi
                    EndDate = startDate.AddDays(7),  // 7 gün sonrası
                    CustomerId = request.TenantCustomerId,
                    DurationInMonths = 0,
                };

                // Lisans kaydını veritabanına ekle
                await _tenantContext.Licenses.AddAsync(license);

                // Lisans kaydını veritabanına kaydet
                await _tenantContext.SaveChangesAsync();

                // Mağaza üzerinde LicenseIds güncellemesi
                store.LicenseId = license.Id;
                await _tenantContext.SaveChangesAsync();

                // Demo talebini güncelle
                request.DemoStartDate = startDate;
                request.DemoEndDate = startDate.AddDays(request.DemoDays);
                request.ApprovedDate = DateTime.UtcNow;
                request.ApprovedByAdminId = admin?.Id;
                request.RequestStatus = "Tamamlandı";

                // Değişiklikleri kaydet
                await _tenantContext.SaveChangesAsync();
                

                TempData["Success"] = "Demo lisansı başarıyla oluşturuldu.";
                return RedirectToAction("Detail", new { id });
            }

            return BadRequest("Geçersiz talep durumu.");
        }
    }
}