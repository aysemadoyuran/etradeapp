using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]


    public class SizeController : Controller
    {
        private readonly EtradeContext _context;

        public SizeController(IHttpContextAccessor httpContextAccessor)
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

        // Beden listeleme
        public IActionResult SizeAdd()
        {
            var sizes = _context.Sizes.ToList(); // Bedeni tamamen listele
            return View(sizes);
        }

        // Beden ekleme
        [HttpPost]
        public IActionResult Add(Size size)
        {
            if (ModelState.IsValid)
            {
                _context.Sizes.Add(size);
                _context.SaveChanges();
                TempData["Success"] = "Beden başarıyla eklendi.";
                return RedirectToAction("SizeAdd", "Size", new { area = "Admin" });

            }
            TempData["Error"] = "Beden eklenirken bir hata oluştu.";
            return RedirectToAction("SizeAdd", "Size", new { area = "Admin" });
        }

        // Beden güncelleme
        [HttpGet]
        public IActionResult GetSize(int id)
        {
            var size = _context.Sizes.Find(id);
            return Json(size);
        }

        [HttpPost]
        public IActionResult UpdateSize(int id, string name)
        {
            var size = _context.Sizes.FirstOrDefault(c => c.Id == id);
            if (size == null)
            {
                return NotFound();
            }

            size.Name = name;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        // Beden silme
        [HttpPost]
        public IActionResult DeleteSize(int id)
        {
            var size = _context.Sizes.Find(id);
            if (size == null)
            {
                return Json(new { success = false, message = "Beden bulunamadı." });
            }

            _context.Sizes.Remove(size);
            _context.SaveChanges();
            return Json(new { success = true });
        }
    }
}
