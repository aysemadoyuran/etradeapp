using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using etrade.Models;
using etrade.Data;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using etrade.Data.Concrete;

namespace etrade.Areas.Aysoft.Controllers
{
    [Area("Tenant")]
    public class ActiveController : Controller
    {
        private readonly ILogger<ActiveController> _logger;
        private readonly TenantContext _context;
        private readonly IAntiforgery _antiforgery;

        public ActiveController(
            ILogger<ActiveController> logger, 
            TenantContext context,
            IAntiforgery antiforgery)
        {
            _context = context;
            _logger = logger;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            ViewBag.AntiForgeryToken = tokens.RequestToken;
            return View();
        }

        [HttpGet]
        public IActionResult GetLicenseByCode(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return Json(new { success = false, message = "Kod boş olamaz" });

                var license = _context.Licenses
                    .FirstOrDefault(x => x.FrozenCode == code && x.IsFrozen);
                
                if (license == null)
                    return Json(new { success = false, message = "Geçersiz dondurma kodu" });

                return Json(new
                {
                    success = true,
                    name = license.TenantCustomer?.FullName ?? "Bilinmiyor",
                    frozenDate = license.FreezeDate?.ToString("dd.MM.yyyy HH:mm"),
                    canBeActivatedDate = license.FreezeDate?.AddMonths(1).ToString("dd.MM.yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lisans bilgisi getirilirken hata oluştu");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActivateLicense([FromBody] ActivateRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                    return Json(new { success = false, message = "Kod boş olamaz" });

                if (request.Months <= 0 || request.Months > 36)
                    return Json(new { success = false, message = "Geçersiz ay sayısı" });

                var license = _context.Licenses
                    .FirstOrDefault(x => x.FrozenCode == request.Code && x.IsFrozen);
                
                if (license == null)
                    return Json(new { success = false, message = "Lisans bulunamadı veya zaten aktif" });

                // Minimum bekleme süresi kontrolü
                if (license.FreezeDate?.AddMonths(1) > DateTime.Now)
                    return Json(new { 
                        success = false, 
                        message = $"Lisansınızı {license.FreezeDate?.AddMonths(1):dd.MM.yyyy} tarihinden önce aktifleştiremezsiniz" 
                    });

                // Lisansı aktifleştir
                license.IsFrozen = false;
                license.FrozenCode = null;
                license.StartDate = DateTime.Now;
                license.EndDate = DateTime.Now.AddMonths(request.Months);
                license.ActiveDate = DateTime.Now;

                _context.SaveChanges();

                // Loglama
                _logger.LogInformation($"License activated: {license.Id} for {request.Months} months");

                return Json(new { 
                    success = true,
                    message = "Lisans başarıyla aktifleştirildi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lisans aktifleştirilirken hata oluştu");
                return Json(new { 
                    success = false, 
                    message = "Bir hata oluştu: " + ex.Message 
                });
            }
        }
    }

    public class ActivateRequest
    {
        public string Code { get; set; }
        public int Months { get; set; }
    }
}