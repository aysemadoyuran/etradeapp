using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Areas.Admin.Services;
using etrade.Areas.Shop.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]


    public class HomeController : Controller
    {
        private readonly EtradeContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CachedStoreSettingsService _settingsService;




        public HomeController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, CachedStoreSettingsService settingsService)
        {
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
            _webHostEnvironment = webHostEnvironment;
            _settingsService = settingsService;
        }
        public async Task<IActionResult> GetStoreJsonSettings()
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();
                return Ok(jsonObj.ToObject<StoreSettingsModel>());
            }
            catch (FileNotFoundException ex)
            {
                return BadRequest($"Dosya bulunamadı: {ex.Message}");
            }
            catch (JsonException ex)
            {
                return BadRequest($"JSON deserialization hatası: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Genel hata: {ex.Message}");
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetTopBarLinks()
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();
                var topbar = jsonObj["Topbar"]?["Links"];

                if (topbar == null)
                    return NotFound("Topbar links not found");

                return Ok(topbar.ToObject<List<LinkModel>>());
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
        public IActionResult index()
        {
            return View();
        }
        public IActionResult Notifications()
        {
            return View();
        }
        public IActionResult GetUnreadNotifications()
        {
            try
            {
                // Toplam okunmamış bildirim sayısı
                var totalUnreadCount = _context.Notifications
                    .Count(n => !n.IsRead && n.UserId == GetCurrentUserId());
                Console.WriteLine($"Toplam okunmamış bildirim sayısı: {totalUnreadCount}");

                // Son 7 okunmamış bildirim
                var lastUnreadNotifications = _context.Notifications
                    .Where(n => !n.IsRead && n.UserId == GetCurrentUserId())
                    .OrderByDescending(n => n.Id)
                    .Take(7)
                    .Select(n => new
                    {
                        n.Id,
                        n.Message,
                        n.NotificationType,
                        n.CreatedAt
                    })
                    .ToList();

                return Ok(new
                {
                    totalUnreadCount,
                    notifications = lastUnreadNotifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Tüm bildirimleri getir (sondan başa)
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
    [FromQuery] string filter = "all",
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();

            // Kullanıcının okunmamış bildirimlerini okundu olarak işaretle
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            var query = _context.Notifications.Where(n => n.UserId == userId);

            // Eski bildirimleri sil (son 20'den fazlasını)
            var totalNotifications = await query.CountAsync();
            if (totalNotifications > 20)
            {
                var oldNotifications = await query
                    .OrderByDescending(n => n.Id) // En yeni bildirimler en üstte olacak
                    .Skip(20) // Son 20'yi koruyarak geri kalanları al
                    .ToListAsync();

                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();
            }

            // Filtreleme işlemi
            if (filter.Equals("unread", StringComparison.OrdinalIgnoreCase))
                query = query.Where(n => !n.IsRead);
            else if (filter.Equals("read", StringComparison.OrdinalIgnoreCase))
                query = query.Where(n => n.IsRead);

            // Sondan başa sıralama
            query = query.OrderByDescending(n => n.Id);

            // Sayfalama
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.NotificationType,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedAt
                })
                .ToListAsync();

            return Ok(new { items, totalCount });
        }


        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        public async Task<IActionResult> GetStoreLogos()
        {
            var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync(ss => ss.Id == 1);
            if (storeSetting == null)
            {
                return NotFound("Logo ayarları bulunamadı.");
            }

            return Ok(new
            {
                logoDarkPath = storeSetting.LogoPath,
                logoLightPath = storeSetting.LogoWhitePath

            });
        }
        public async Task<IActionResult> GetFooterAndSiteMapAndPolicies()
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // JSON'u StoreSettingsModel'e deserialize ediyoruz
                var storeSettings = jsonObj.ToObject<StoreSettingModel>();

                // Veriyi döndürüyoruz
                return Ok(storeSettings);
            }
            catch (Exception ex)
            {
                // Hata durumunda mesaj dönüyoruz
                return StatusCode(500, new { message = "Veri çekme hatası: " + ex.Message });
            }
        }
        public IActionResult PageNotFound()
        {
            return View();
        }
    }


    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }


}
