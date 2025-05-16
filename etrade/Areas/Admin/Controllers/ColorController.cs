using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using etrade.Data;
using etrade.Entity;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Authorization;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]

    public class ColorController : Controller
    {
        private readonly EtradeContext _context;

        public ColorController(IHttpContextAccessor httpContextAccessor)
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
        }

        // Renk listeleme
        public IActionResult ColorAdd()
        {
            var colors = _context.Colors.ToList(); // Tüm renkleri listele

            return View(colors);
        }

        // Renk ekleme (POST)
        [HttpPost]
        public IActionResult Add(Color color)
        {
            if (ModelState.IsValid)
            {
                // Yeni rengi veritabanına ekliyoruz
                _context.Colors.Add(color);
                _context.SaveChanges();

                // Başarılı mesajı göster
                TempData["Success"] = "Renk başarıyla eklendi.";
                return RedirectToAction("ColorAdd");
            }

            // Hata durumunda mesaj göster
            TempData["Error"] = "Renk eklenirken bir hata oluştu.";
            return RedirectToAction("ColorAdd", "Color", new { area = "Admin" });
        }

        // Renk güncelleme (GET - Modal için)
        [HttpGet]
        public IActionResult GetColor(int id)
        {
            var color = _context.Colors.Find(id);
            if (color == null)
            {
                return Json(new { success = false, message = "Renk bulunamadı." });
            }

            // Renk verisini modaldeki form alanlarına göndermek için Json formatında geri döner
            return Json(color);
        }

        // Renk güncelleme (POST)
        [HttpPost]
        public IActionResult UpdateColor(int id, string name, string colorCode)
        {
            var color = _context.Colors.FirstOrDefault(c => c.Id == id);
            if (color == null)
            {
                return Json(new { success = false, message = "Renk bulunamadı." });
            }

            // Renk adı ve renk kodunu güncelliyoruz
            color.Name = name;
            color.ColorCode = colorCode;
            _context.SaveChanges();

            // Güncelleme başarılı ise success mesajı döndür
            return Json(new { success = true });
        }

        // Renk silme (POST)
        [HttpPost]
        public IActionResult DeleteColor(int id)
        {
            var color = _context.Colors.Find(id);
            if (color == null)
            {
                return Json(new { success = false, message = "Renk bulunamadı." });
            }

            // Rengi veritabanından siliyoruz
            _context.Colors.Remove(color);
            _context.SaveChanges();

            // Silme başarılı ise success mesajı döndür
            return Json(new { success = true });
        }

        // Anasayfa
        public IActionResult Index()
        {
            return View();
        }
    }
}
