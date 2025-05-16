using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace etrade.Areas.Admin.Services
{
    public class CachedStoreSettingsService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly EtradeContext _context;
        private const string CacheKeyPrefix = "StoreSettings_";

        public CachedStoreSettingsService(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _webHostEnvironment = webHostEnvironment;
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

        private async Task<string> GetJsonContentAsync()
        {
            var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
            if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                throw new FileNotFoundException("Store settings or JSON path not found");

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));
            return await System.IO.File.ReadAllTextAsync(fullPath);
        }

        public async Task<JObject> GetCachedJsonAsync()
        {
            string cacheKey = $"{CacheKeyPrefix}FullJson";
            return await CacheHelper.GetOrCreate(cacheKey, async () =>
            {
                var content = await GetJsonContentAsync();
                return JObject.Parse(content);
            });
        }

        public void InvalidateCache()
        {
            // Tüm store settings cache'lerini temizle
            var cacheKeys = new List<string>
            {
            $"{CacheKeyPrefix}FullJson",
            $"{CacheKeyPrefix}TopbarLinks",
            $"{CacheKeyPrefix}MobileMenu",
            $"{CacheKeyPrefix}FooterData",
            $"{CacheKeyPrefix}InvoiceInfo" 
            };

            foreach (var key in cacheKeys)
            {
                CacheHelper.Remove(key);  // Cache'ten her bir anahtarı sil
            }
        }
    }
}
