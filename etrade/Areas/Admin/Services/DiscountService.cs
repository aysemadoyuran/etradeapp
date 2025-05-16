using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static etrade.Areas.Tenant.Controllers.StoreController;

public class DiscountService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private Timer? _timer;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(IServiceScopeFactory serviceScopeFactory, ILogger<DiscountService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim Servisi başlatılıyor...");
        _timer = new Timer(ExecuteTask, null, TimeSpan.Zero, _interval);
        return Task.CompletedTask;
    }

    private void ExecuteTask(object state)
    {
        _logger.LogInformation("İndirim kontrolü başlatıldı: " + DateTime.Now);

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
            var tenants = tenantContext.TenantStores.AsNoTracking().ToList();

            foreach (var tenant in tenants)
            {
                try
                {
                    _logger.LogInformation($"[{tenant.StoreName}] tenant için indirim işlemleri başlatılıyor");

                    // Şifrelenmiş bağlantı string'ini çöz
                    var decryptedConnectionString = EncryptionHelper.Decrypt(tenant.ConnectionString);

                    var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
                    optionsBuilder.UseMySql(decryptedConnectionString, new MySqlServerVersion(new Version(8, 0, 40)));

                    using var dbContext = new EtradeContext(optionsBuilder.Options);
                    ProcessDiscountsForTenant(dbContext, tenant.StoreName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{tenant.StoreName}] tenant işlenirken beklenmeyen hata");
                }
            }
        }
    }

    private void ProcessDiscountsForTenant(EtradeContext dbContext, string storeName)
    {
        var currentDateTime = DateTime.Now;

        var discountsToActivate = dbContext.Discounts
            .Where(d => d.StartDateTime <= currentDateTime &&
                       d.EndDateTime >= currentDateTime &&
                       !d.IsActive)
            .AsNoTracking()
            .ToList();

        foreach (var discount in discountsToActivate)
        {
            _logger.LogInformation($"[{storeName}] {discount.Id} ID'li indirim aktif ediliyor...");
            ActivateDiscount(discount, dbContext, storeName);
        }

        var discountsToDeactivate = dbContext.Discounts
            .Where(d => d.EndDateTime < currentDateTime && d.IsActive)
            .AsNoTracking()
            .ToList();

        foreach (var discount in discountsToDeactivate)
        {
            _logger.LogInformation($"[{storeName}] {discount.Id} ID'li indirim süresi doldu, pasif ediliyor...");
            DeactivateDiscount(discount, dbContext, storeName);
        }
    }

    private void ActivateDiscount(Discount discount, EtradeContext dbContext, string storeName)
    {
        using var transaction = dbContext.Database.BeginTransaction();

        try
        {
            if (discount.ConditionType == ConditionType.Product)
            {
                var discountProducts = dbContext.DiscountProducts
                    .Where(dp => dp.DiscountId == discount.Id)
                    .Include(dp => dp.Product)
                    .ToList();

                foreach (var dp in discountProducts)
                {
                    if (dp.Product == null) continue;

                    if (!dp.Product.OriginalPrice.HasValue || dp.Product.OriginalPrice.Value <= 0)
                    {
                        dp.Product.OriginalPrice = dp.Product.Price;
                    }

                    decimal discountAmount = discount.DiscountType == DiscountType.Percentage
                        ? dp.Product.OriginalPrice.Value * (discount.Value / 100)
                        : discount.Value;

                    decimal newPrice = dp.Product.OriginalPrice.Value - discountAmount;

                    if (newPrice < dp.Product.Price || dp.Product.DiscountId == null)
                    {
                        dp.Product.Price = newPrice;
                        dp.Product.DiscountId = discount.Id;
                        dbContext.Products.Update(dp.Product);
                        _logger.LogInformation($"[{storeName}] {dp.Product.Id} ID'li ürün fiyatı güncellendi: {newPrice}₺");
                    }
                }
            }
            else if (discount.ConditionType == ConditionType.Category)
            {
                var discountCategories = dbContext.DiscountCategories
                    .Where(dc => dc.DiscountId == discount.Id)
                    .Include(dc => dc.Category)
                    .ToList();

                foreach (var dc in discountCategories)
                {
                    if (dc.Category == null) continue;

                    var categoryProducts = dbContext.Products
                        .Where(p => p.CategoryId == dc.CategoryId)
                        .ToList();

                    foreach (var product in categoryProducts)
                    {
                        if (!product.OriginalPrice.HasValue || product.OriginalPrice.Value <= 0)
                        {
                            product.OriginalPrice = product.Price;
                        }

                        decimal discountAmount = discount.DiscountType == DiscountType.Percentage
                            ? product.OriginalPrice.Value * (discount.Value / 100)
                            : discount.Value;

                        decimal newPrice = product.OriginalPrice.Value - discountAmount;

                        if (newPrice < product.Price || product.DiscountId == null)
                        {
                            product.Price = newPrice;
                            product.DiscountId = discount.Id;
                            dbContext.Products.Update(product);
                            _logger.LogInformation($"[{storeName}] {product.Id} ID'li ürün fiyatı güncellendi: {newPrice}₺");
                        }
                    }
                }
            }

            discount.IsActive = true;
            dbContext.Discounts.Update(discount);
            dbContext.SaveChanges();

            SendDiscountNotifications(discount, dbContext, storeName);

            transaction.Commit();
            _logger.LogInformation($"[{storeName}] {discount.Id} ID'li indirim başarıyla aktif edildi");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, $"[{storeName}] {discount.Id} ID'li indirim aktif edilirken hata");
            throw;
        }
    }

    private void DeactivateDiscount(Discount discount, EtradeContext dbContext, string storeName)
    {
        using var transaction = dbContext.Database.BeginTransaction();

        try
        {
            if (discount.ConditionType == ConditionType.Product)
            {
                var discountProducts = dbContext.DiscountProducts
                    .Where(dp => dp.DiscountId == discount.Id)
                    .Include(dp => dp.Product)
                    .ToList();

                foreach (var dp in discountProducts)
                {
                    if (dp.Product == null || !dp.Product.OriginalPrice.HasValue) continue;

                    dp.Product.Price = dp.Product.OriginalPrice.Value;
                    dp.Product.OriginalPrice = null;
                    dp.Product.DiscountId = null;
                    dbContext.Products.Update(dp.Product);
                }
            }
            else if (discount.ConditionType == ConditionType.Category)
            {
                var discountCategories = dbContext.DiscountCategories
                    .Where(dc => dc.DiscountId == discount.Id)
                    .Include(dc => dc.Category)
                    .ToList();

                foreach (var dc in discountCategories)
                {
                    if (dc.Category == null) continue;

                    var categoryProducts = dbContext.Products
                        .Where(p => p.CategoryId == dc.CategoryId && p.DiscountId == discount.Id)
                        .ToList();

                    foreach (var product in categoryProducts)
                    {
                        if (!product.OriginalPrice.HasValue) continue;

                        product.Price = product.OriginalPrice.Value;
                        product.OriginalPrice = null;
                        product.DiscountId = null;
                        dbContext.Products.Update(product);
                    }
                }
            }

            discount.IsActive = false;
            dbContext.Discounts.Update(discount);
            dbContext.SaveChanges();

            transaction.Commit();
            _logger.LogInformation($"[{storeName}] {discount.Id} ID'li indirim başarıyla pasif edildi");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, $"[{storeName}] {discount.Id} ID'li indirim pasif edilirken hata");
            throw;
        }
    }

    private void SendDiscountNotifications(Discount discount, EtradeContext dbContext, string storeName)
    {
        try
        {
            var discountMessage = discount.DiscountType == DiscountType.Percentage
                ? $"Yeni indirim! {discount.Value:0} % indirim fırsatını kaçırmayın!" // Yüzde ise virgülden sonra basamağı göstermiyoruz
                : $"Yeni indirim! {discount.Value:0.00}₺ indirim fırsatını kaçırmayın!"; // TL ise virgülden sonra 2 basamağı gösteriyoruz

            // Tüm kullanıcılara genel bildirim
            var allUserIds = dbContext.Users.Select(u => u.Id).ToList();

            foreach (var userId in allUserIds)
            {
                dbContext.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Yeni İndirim!",
                    Message = discountMessage,
                    NotificationType = "discount",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            // Favori ürünü indirime giren kullanıcılar
            var favoriteUserIds = GetUsersWithFavoriteItemsOnDiscount(discount, dbContext);

            foreach (var userId in favoriteUserIds)
            {
                dbContext.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Favorinizdeki ürün indirime girdi!",
                    Message = "Favorilerinize eklediğiniz ürünlerden bazıları indirime girdi. Hemen kontrol edin!",
                    NotificationType = "personal_discount",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            dbContext.SaveChanges();
            _logger.LogInformation($"[{storeName}] {discount.Id} ID'li indirim için bildirimler gönderildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[{storeName}] Bildirim gönderilirken hata oluştu");
        }
    }

    private List<string> GetUsersWithFavoriteItemsOnDiscount(Discount discount, EtradeContext dbContext)
    {
        var userIds = new List<string>();

        if (discount.ConditionType == ConditionType.Product)
        {
            var productIds = dbContext.DiscountProducts
                .Where(dp => dp.DiscountId == discount.Id)
                .Select(dp => dp.ProductId)
                .ToList();

            userIds = dbContext.Favorites
                .Where(f => productIds.Contains(f.ProductId))
                .Select(f => f.UserId)
                .Distinct()
                .ToList();
        }
        else if (discount.ConditionType == ConditionType.Category)
        {
            var categoryIds = dbContext.DiscountCategories
                .Where(dc => dc.DiscountId == discount.Id)
                .Select(dc => dc.CategoryId)
                .ToList();

            var productIds = dbContext.Products
                .Where(p => categoryIds.Contains(p.CategoryId))
                .Select(p => p.Id)
                .ToList();

            userIds = dbContext.Favorites
                .Where(f => productIds.Contains(f.ProductId))
                .Select(f => f.UserId)
                .Distinct()
                .ToList();
        }

        return userIds;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("İndirim Servisi durduruluyor...");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _logger.LogInformation("İndirim Servisi dispose edildi");
    }
}