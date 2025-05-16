using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace etrade.Areas.Admin.Services
{
    public static class CacheHelper
    {
        private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        public static T GetOrCreate<T>(string cacheKey, Func<T> getItemCallback)
        {
            if (!_cache.TryGetValue(cacheKey, out T cachedItem))
            {
                cachedItem = getItemCallback();
                _cache.Set(cacheKey, cachedItem, _cacheDuration);
            }
            return cachedItem;
        }

        public static void Remove(string cacheKey)
        {
            _cache.Remove(cacheKey);
        }
    }
}