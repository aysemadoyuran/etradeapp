using System;
using System.Linq;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Tenant.Middlewares
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TenantMiddleware> _logger;

        public TenantMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            ILogger<TenantMiddleware> logger)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("TenantMiddleware başladı - {Path}", context.Request.Path);

            if (context.Request.Path.StartsWithSegments("/Identity") ||
                context.Request.Path.StartsWithSegments("/Tenant") ||
                context.Request.Path.StartsWithSegments("/Aysoft"))
            {
                _logger.LogInformation("Özel route için middleware atlandı");
                await _next(context);
                return;
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
                var tenant = tenantService.GetTenantByDomain(context.Request.Host.Host);

                if (tenant != null)
                {
                    _logger.LogInformation("Tenant bulundu: {TenantName} ({Domain}) - {Path}",
                        tenant.StoreName, tenant.Domain, context.Request.Path);

                    var isAdminRoute = context.Request.Path.StartsWithSegments("/Admin");

                    // ❌ Lisans tamamen feshedildiyse
                    if (tenant.LicenseId == null)
                    {
                        _logger.LogWarning("Feshedilmiş lisans için erişim engellendi - {Domain}", tenant.Domain);

                        // Kullanıcının zaten hata sayfasında olup olmadığını kontrol et
                        if (!context.Request.Path.StartsWithSegments("/Admin/Hata/LisansYok"))
                        {
                            context.Response.Redirect("/Admin/Hata/LisansYok");
                            return;
                        }
                    }

                    // ❌ Manuel olarak erişim engellenmişse
                    if (!tenant.Database)
                    {
                        if (!isAdminRoute)
                        {
                            _logger.LogWarning("Manuel erişim engeli aktif - {Domain}", tenant.Domain);

                            // Kullanıcının zaten erişim kısıtlaması sayfasında olup olmadığını kontrol et
                            if (!context.Request.Path.StartsWithSegments("/Admin/Hata/ErisimKisitlamasi"))
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.Redirect("/Admin/Hata/ErisimKisitlamasi");
                                await context.Response.WriteAsync("Mağaza erişiminiz yönetici tarafından geçici olarak durdurulmuştur.");
                                return;
                            }
                        }
                    }

                    // 🧾 Ödeme kontrolü - geçmiş ödemelere toleranslı bak
                    var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
                    var unpaidPastPayments = await tenantDbContext.LicensePayments
                        .Where(lp => lp.LicenseId == tenant.LicenseId &&
                                     !lp.IsPaid &&
                                     lp.EndPeriod.AddDays(7).Date < DateTime.UtcNow.Date)
                        .ToListAsync();

                    if (unpaidPastPayments.Any())
                    {
                        if (!isAdminRoute)
                        {
                            _logger.LogWarning("Geçmiş ödemeler eksik - erişim kapatıldı - {Domain}", tenant.Domain);

                            // Kullanıcının zaten ödeme sayfasında olup olmadığını kontrol et
                            if (!context.Request.Path.StartsWithSegments("/Admin/Hata/Odeme"))
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.Redirect("/Admin/Hata/Odeme");
                                return;
                            }
                        }
                    }

                    // 🔒 İptal talebi kontrolü - Shop alanı için erişimi kes
                    if (context.Request.Path.StartsWithSegments("/Shop"))
                    {
                        var activeCancellationRequest = await tenantDbContext.CancellationRequests
                            .FirstOrDefaultAsync(r =>
                                r.LicenseId == tenant.LicenseId);  // Lisansa ait herhangi bir talep varsa

                        if (activeCancellationRequest != null)
                        {
                            _logger.LogWarning("İptal sürecinde - erişim Shop alanı için kapatıldı - {Domain}", tenant.Domain);

                            // Kullanıcıyı uyarıcı sayfaya yönlendir
                            if (!context.Request.Path.StartsWithSegments("/Admin/Hata/ErisimKisitlamasi"))
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.Redirect("/Admin/Hata/ErisimKisitlamasi");
                                return;
                            }
                        }
                    }

                    // 🔒 Lisans sonlandırma kontrolü – Tüm sistem erişimini kes
                    var license = await tenantDbContext.Licenses
                        .FirstOrDefaultAsync(l => l.Id == tenant.LicenseId);

                    if (license != null && license.IsDeleted)
                    {
                        _logger.LogWarning("Lisans sonlandırılmış - tüm sistem erişimi engellendi - {Domain}", tenant.Domain);

                        // Eğer hata sayfasında değilse yönlendir
                        if (!context.Request.Path.StartsWithSegments("/Admin/Hata/LisansSonlandirildi"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.Redirect("/Admin/Hata/LisansSonlandirildi");
                            return;
                        }
                    }

                    if (license != null && license.IsFrozen)
                    {
                        _logger.LogWarning("Lisans Dondurulmuş - tüm sistem erişimi engellendi - {Domain}", tenant.Domain);

                        // Eğer hata sayfasında değilse yönlendir
                        if (!context.Request.Path.StartsWithSegments("/Admin/Hata/LisansDonduruldu"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.Redirect("/Admin/Hata/LisansDonduruldu");
                            return;
                        }
                    }

                    // ❌ Lisansın geçerlilik süresi geçmişse
                    if (license != null && license.EndDate < DateTime.UtcNow)
                    {
                        _logger.LogWarning("Lisans süresi dolmuş - erişim engellendi - {Domain}", tenant.Domain);

                        // Eğer hata sayfasında değilse yönlendir
                        if (!context.Request.Path.StartsWithSegments("/Admin/Hata/LisansGunuDoldu"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.Redirect("/Admin/Hata/LisansGunuDoldu");
                            return;
                        }
                    }

                    try
                    {
                        var decryptedConnectionString = EncryptionHelper.Decrypt(tenant.ConnectionString);

                        var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
                        optionsBuilder.UseMySql(decryptedConnectionString,
                            new MySqlServerVersion(new Version(8, 0, 40)));

                        var dbContext = new EtradeContext(optionsBuilder.Options);

                        await dbContext.Database.CanConnectAsync();

                        context.Items["DbContext"] = dbContext;
                        _logger.LogInformation("DbContext başarıyla oluşturuldu - Tenant: {TenantName} ({Domain})", tenant.StoreName, tenant.Domain);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Tenant veritabanına bağlanırken hata oluştu - Tenant: {TenantName} ({Domain})", tenant.StoreName, tenant.Domain);
                        await CreateFallbackContext(context, scope);
                    }
                }
                else
                {
                    _logger.LogWarning("Tenant bulunamadı: {Domain} - {Path}", context.Request.Host.Host, context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Hatalı URL, böyle bir mağaza bulunamadı.");
                    return;
                }
            }

            await _next(context);
            _logger.LogInformation("TenantMiddleware tamamlandı - {Path}", context.Request.Path);
        }

        private async Task CreateFallbackContext(HttpContext context, IServiceScope scope)
        {
            try
            {
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
                optionsBuilder.UseMySql(configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 40)));

                var dbContext = new EtradeContext(optionsBuilder.Options);
                await dbContext.Database.CanConnectAsync();

                context.Items["DbContext"] = dbContext;
                _logger.LogInformation("Varsayılan DbContext oluşturuldu ve kaydedildi");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Varsayılan veritabanına bağlanılamadı!");
                throw;
            }
        }
    }
}