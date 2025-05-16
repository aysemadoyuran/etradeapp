using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Admin.Services
{
    public class CouponExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CouponExpirationService> _logger;
        private const int CheckIntervalMinutes = 1440;
        public CouponExpirationService(
            IServiceProvider serviceProvider,
            ILogger<CouponExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("CouponExpirationService initialized");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Coupon expiration check service starting...");

            // İlk çalışmada hemen kontrol et
            await CheckAllTenantCoupons(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"Next coupon check in {CheckIntervalMinutes} minutes...");

                    // Belirtilen aralıkta bekle
                    await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);

                    // Tüm tenant'lar için kupon kontrolü yap
                    await CheckAllTenantCoupons(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Coupon check task was canceled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in coupon expiration service main loop");

                    // Hata durumunda 5 dakika bekle
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Coupon expiration check service stopping...");
        }

        private async Task CheckAllTenantCoupons(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting coupon check for all tenants...");

            using var scope = _serviceProvider.CreateScope();
            var centralContext = scope.ServiceProvider.GetRequiredService<TenantContext>();

            try
            {
                var tenants = await centralContext.TenantStores
                    .AsNoTracking()
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"Found {tenants.Count} tenants to check");

                foreach (var tenant in tenants)
                {
                    try
                    {
                        await CheckTenantCoupons(tenant, stoppingToken);
                    }
                    catch (Exception exTenant)
                    {
                        _logger.LogError(exTenant,
                            $"[{tenant.StoreName}] Error checking coupons. TenantId: {tenant.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tenant list from central database");
            }
        }

        private async Task CheckTenantCoupons(TenantStore tenant, CancellationToken stoppingToken)
        {
            _logger.LogInformation($"[{tenant.StoreName}] Starting coupon check...");

            try
            {
                var decryptedConnectionString = EncryptionHelper.Decrypt(tenant.ConnectionString);

                var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
                optionsBuilder.UseMySql(
                    decryptedConnectionString,
                    new MySqlServerVersion(new Version(8, 0, 40)));

                using var tenantContext = new EtradeContext(optionsBuilder.Options);

                var expiredCoupons = await tenantContext.Coupons
                    .Where(c => c.IsActive && c.EndDate < DateTime.Now)
                    .ToListAsync(stoppingToken);

                _logger.LogInformation($"[{tenant.StoreName}] Found {expiredCoupons.Count} expired coupons");

                if (expiredCoupons.Any())
                {
                    foreach (var coupon in expiredCoupons)
                    {
                        coupon.IsActive = false;
                        _logger.LogInformation($"[{tenant.StoreName}] Deactivating coupon: {coupon.Code}");
                    }

                    var affected = await tenantContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation(
                        $"[{tenant.StoreName}] Successfully deactivated {affected} coupons");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{tenant.StoreName}] Error in tenant coupon check");
                throw;
            }
        }
    }
}