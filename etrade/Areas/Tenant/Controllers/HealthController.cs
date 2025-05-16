using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]

    public class HealthController : Controller
    {
        private readonly ILogger<HealthController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;

        // Controller için constructor (yapıcı metod)
        public HealthController(ILogger<HealthController> logger, TenantContext tenantContext, TenantService tenantService)
        {
            _logger = logger;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
        }

        // Veritabanının kapsamlı sağlık durumunu döndüren metot
        public async Task<IActionResult> GetComprehensiveDatabaseHealthStatus()
        {
            // Tenant (kiracı) bilgilerini al
            var domains = await _tenantContext.TenantStores
                                              .Where(t => t.Database)  // Veritabanı bilgisi olanları seç
                                              .Select(t => new { t.Id, t.Domain })  // Sadece Id ve Domain alanlarını seç
                                              .ToListAsync();

            var healthStatus = new List<object>();

            // Her bir domain için sağlık durumu kontrolü yap
            foreach (var domain in domains)
            {
                // Veritabanı bağlantısını kontrol et
                var connectionStatus = await CheckDatabaseConnectionAsync(_tenantService.GetTenantConnectionString(domain.Id));
                // Migrasyonların uygulanıp uygulanmadığını kontrol et
                var migrationsStatus = await CheckMigrationsAppliedAsync(domain.Id);
                // Veritabanı performansını kontrol et
                var performanceStatus = await CheckDatabasePerformanceAsync(domain.Id);
                // Veritabanı hata günlüklerini al

                // Sağlık durumu bilgisini listeye ekle
                healthStatus.Add(new
                {
                    TenantId = domain.Id,
                    Domain = domain.Domain,
                    ConnectionStatus = connectionStatus ? "Bağlantı Başarılı" : "Bağlantı Başarısız",
                    MigrationStatus = migrationsStatus ? "Migrasyon Tamamlandı" : "Migrasyon Bekliyor",
                    PerformanceStatus = performanceStatus,
                });
            }

            // JSON olarak sağlık durumunu döndür
            return Json(healthStatus);
        }

        // Veritabanı bağlantısını kontrol eden metod
        public async Task<bool> CheckDatabaseConnectionAsync(string connectionString)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
                    .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));

                using (var context = new EtradeContext(optionsBuilder.Options))
                {
                    // Basit bir sorgu ile bağlantıyı test et
                    await context.Database.ExecuteSqlRawAsync("SELECT 1");
                }

                return true; // Bağlantı başarılıysa true döndür
            }
            catch (Exception ex)
            {
                _logger.LogError($"Veritabanı bağlantısı sırasında hata: {ex.Message}");
                return false; // Bağlantı hatalıysa false döndür
            }
        }

        // Migrasyonların uygulanıp uygulanmadığını kontrol eden metod
        public async Task<bool> CheckMigrationsAppliedAsync(int tenantId)
        {
            try
            {
                var connectionString = _tenantService.GetTenantConnectionString(tenantId);
                var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
                    .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));

                using (var context = new EtradeContext(optionsBuilder.Options))
                {
                    // Uygulanmamış migrasyonları al
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

                    if (pendingMigrations.Any())
                    {
                        // Bekleyen migrasyonları logla
                        _logger.LogWarning($"Uygulanmamış migrasyonlar: {string.Join(", ", pendingMigrations)}");
                        return false; // Eğer uygulanmamış migrasyon varsa false döndür
                    }

                    return true; // Eğer tüm migrasyonlar uygulanmışsa true döndür
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Migrasyon kontrolü sırasında hata: {ex.Message}");
                return false; // Hata durumunda false döndür
            }
        }

        // Veritabanı performansını kontrol eden metod
        public async Task<string> CheckDatabasePerformanceAsync(int tenantId)
        {
            try
            {
                var connectionString = _tenantService.GetTenantConnectionString(tenantId);
                var stopwatch = Stopwatch.StartNew();

                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }

                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (elapsedMs < 100)
                    return $"Mükemmel ({elapsedMs} ms)";
                else if (elapsedMs < 300)
                    return $"İyi ({elapsedMs} ms)";
                else
                    return $"Yavaş ({elapsedMs} ms)";
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }
        [HttpPost]
        public IActionResult RunMigrationsForAllTenants()
        {
            var migrationResults = new List<string>();
            var tenants = _tenantService.GetAllTenantConnectionStrings();

            foreach (var tenant in tenants)
            {
                try
                {
                    var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();

                    optionsBuilder.UseMySql(
                        tenant.ConnectionString,
                        new MySqlServerVersion(new Version(8, 0, 40)) // versiyon buraya dikkat
                    );

                    using var context = new EtradeContext(optionsBuilder.Options);
                    context.Database.Migrate();

                    migrationResults.Add($"✅ Migration başarılı: {tenant.Name} (ID: {tenant.TenantId})");
                }
                catch (Exception ex)
                {
                    migrationResults.Add($"❌ Migration HATA: {tenant.Name} (ID: {tenant.TenantId}) - {ex.Message}");
                }
            }

            return Ok(new
            {
                Success = true,
                Timestamp = DateTime.Now,
                Results = migrationResults
            });
        }

        // Sağlık durumu sayfasını döndüren metod
        public IActionResult Index()
        {
            return View(); // Sağlık durumu görünümünü döndür
        }

    }
}