using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]

    public class DropController : Controller
    {
        private readonly ILogger<HealthController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;
        private readonly LicenseSettingsService _licenseSettingsService;


        // Controller için constructor (yapıcı metod)
        public DropController(ILogger<HealthController> logger, TenantContext tenantContext, TenantService tenantService, LicenseSettingsService licenseSettingsService)
        {
            _logger = logger;
            _licenseSettingsService = licenseSettingsService;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
        }
        public async Task<IActionResult> GetDeletedLicenses()
        {
            var now = DateTime.UtcNow;

            var deletedLicenses = await _tenantContext.Licenses
                .Where(l => l.IsDeleted && l.DeletionDate != null)
                .Include(l => l.TenantCustomer)
                .Include(l => l.TenantStores)
                .Select(l => new
                {
                    LicenseId = l.Id,
                    CompanyName = l.TenantCustomer.CompanyName,
                    CustomerName = l.TenantCustomer.FullName,
                    Email = l.TenantCustomer.Email,
                    Domain = l.TenantStores.FirstOrDefault().Domain, // varsa ilk domain'i alıyoruz
                    DeletionDate = l.DeletionDate,
                    DaysSinceDeleted = EF.Functions.DateDiffDay(l.DeletionDate.Value, now),
                    CanBeDeleted = EF.Functions.DateDiffDay(l.DeletionDate.Value, now) >= 30
                })
                .ToListAsync();

            return Ok(deletedLicenses);
        }
        public async Task<IActionResult> PermanentlyDeleteLicense(int id)
        {
            var license = await _tenantContext.Licenses
                .Include(l => l.TenantStores)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsDeleted);

            if (license == null)
                return NotFound("Lisans bulunamadı veya zaten silinmiş.");

            var now = DateTime.UtcNow;
            var days = (now - license.DeletionDate.Value).TotalDays;

            if (days < 30)
                return BadRequest("Bu lisans henüz 30 gün dolmadığı için silinemez.");

            // 1. İlişkili mağazaların LicenseId'lerini null yap
            foreach (var store in license.TenantStores)
            {
                store.LicenseId = null;
            }

            // 2. Faturalandırılmamış (ödeme alınmamış) kayıtları sil
            var unpaidPayments = license.Payments
                .Where(p => !p.IsPaid)
                .ToList();

            _tenantContext.RemoveRange(unpaidPayments);

            // 3. Lisansı da sil (istersen hard delete değil, bir flag daha da ekleyebiliriz)
            _tenantContext.Licenses.Remove(license);

            await _tenantContext.SaveChangesAsync();

            // 4. İlgili mağaza veritabanlarını dropla (opsiyonel, domain'e göre)
            foreach (var store in license.TenantStores)
            {
                var connection = store.ConnectionString;
                //await _databaseService.DropDatabaseAsync(connection); // kendi DB servisin varsa
            }

            return Ok("Lisans kalıcı olarak silindi.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreLicense(int id)
        {
            var license = await _tenantContext.Licenses.FindAsync(id);
            if (license == null)
                return NotFound();

            // 1. İlgili lisansa ait iptal talebini sil
            var cancelRequest = await _tenantContext.CancellationRequests
                .FirstOrDefaultAsync(c => c.LicenseId == id);

            if (cancelRequest != null)
                _tenantContext.CancellationRequests.Remove(cancelRequest);

            // 2. Lisans'ı geri aktif hale getir
            license.IsDeleted = false;

            await _tenantContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Lisans erişime tekrar açıldı." });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteStoreAndDatabase(int id)
        {
            var license = await _tenantContext.Licenses
                .FirstOrDefaultAsync(l => l.Id == id && l.IsDeleted);

            if (license == null)
                return NotFound("Lisans bulunamadı veya silinmemiş.");

            var silinmeTarihi = license.DeletionDate ?? DateTime.MinValue;
            if ((DateTime.Now - silinmeTarihi).TotalDays < 30)
                return BadRequest("Bu lisans henüz 30 günü doldurmadı.");

            // Ödenmemiş license payments kayıtlarını sil
            var unpaidPayments = await _tenantContext.LicensePayments
                .Where(p => p.LicenseId == id && !p.IsPaid)
                .ToListAsync();

            _tenantContext.LicensePayments.RemoveRange(unpaidPayments);

            // Store kaydını bul
            var store = await _tenantContext.TenantStores.FirstOrDefaultAsync(s => s.LicenseId == id);
            if (store != null)
            {
                store.LicenseId = null;
            }

            // TenantStore ile şifreli bağlantı çözülerek database silinir
            var tenantStore = await _tenantContext.TenantStores.FirstOrDefaultAsync(t => t.LicenseId == id);
            if (tenantStore != null)
            {
                try
                {
                    var encryptedConnectionString = tenantStore.ConnectionString;
                    var connectionString = EncryptionHelper.Decrypt(encryptedConnectionString);

                    var builder = new MySqlConnectionStringBuilder(connectionString);
                    var dbName = builder.Database;

                    builder.Database = ""; // master DB gibi davranmak için boş bırakıyoruz

                    using (var connection = new MySqlConnection(builder.ConnectionString))
                    {
                        await connection.OpenAsync();
                        var dropCmd = $"DROP DATABASE `{dbName}`"; // backtick kullanımı MySQL için önemli
                        using (var cmd = new MySqlCommand(dropCmd, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    _tenantContext.TenantStores.Remove(tenantStore);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Veritabanı silinemedi: {ex.Message}");
                }
            }

            if (store != null)
            {
                _tenantContext.TenantStores.Remove(store);
            }

            await _tenantContext.SaveChangesAsync();

            return Ok("Mağaza, ödenmemiş ödemeler ve veritabanı başarıyla silindi. Lisans kaydı sistemde tutulmaya devam ediyor.");
        }
        [HttpGet]
        public async Task<IActionResult> GetTerminatedLicenseDetail(int id)
        {
            var license = await _tenantContext.Licenses
                .Include(l => l.TenantCustomer)
                .FirstOrDefaultAsync(l => l.Id == id && l.IsDeleted);

            if (license == null)
                return Json(new { success = false, message = "Lisans bulunamadı." });

            var payments = await _tenantContext.LicensePayments
                .Where(p => p.LicenseId == id)
                .ToListAsync();

            var cancelRequest = await _tenantContext.CancellationRequests
                .FirstOrDefaultAsync(c => c.LicenseId == id); // varsa

            return Json(new
            {
                success = true,
                license = new
                {
                    startDate = license.StartDate.ToString("yyyy-MM-dd"),
                    endDate = license.EndDate.ToString("yyyy-MM-dd"),
                    deletionDate = license.DeletionDate?.ToString("yyyy-MM-dd")
                },
                customer = new
                {
                    name = license.TenantCustomer?.FullName,
                    address = license.TenantCustomer?.Address,
                    email = license.TenantCustomer?.Email,
                    storename = license.TenantCustomer?.CompanyName

                },
                cancelRequest = cancelRequest != null ? new
                {
                    requestDate = cancelRequest.RequestDate.ToString("yyyy-MM-dd"),
                    description = cancelRequest.Reason
                } : null,
                payments = payments.Select(p => new
                {
                    amount = p.Price,
                    startdate = p.StartPeriod.ToString("yyyy-MM-dd"),
                    enddate = p.EndPeriod.ToString("yyyy-MM-dd"),
                    isPaid = p.IsPaid
                })
            });
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details()
        {
            return View();
        }
    }
}