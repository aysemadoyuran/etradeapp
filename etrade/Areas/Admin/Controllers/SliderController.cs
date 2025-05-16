using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]

    public class SliderController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly EtradeContext _context;


        public SliderController(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor)
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
        }
        public IActionResult GetUserRole()
        {
            var isAdmin = User.IsInRole("admin"); // Kullanıcı Admin mi?
            return Json(new { isAdmin });
        }

        public IActionResult Add()
        {
            return View();
        }
        // ✅ Sliderları Getir
        [HttpGet]
        public IActionResult GetSliders()
        {
            var sliders = _context.Sliders
                .OrderByDescending(s => s.Id) // En son eklenenden ilk eklenene
                .ToList();

            return Json(sliders);
        }

        // ✅ Yeni Slider Ekle
        [HttpPost]
        public async Task<IActionResult> AddSlider(Slider slider, IFormFile Image)
        {
            if (Image != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Image.CopyToAsync(fileStream);
                }

                slider.ImageUrl = "/uploads/" + uniqueFileName;
            }
            else
            {
                return BadRequest("Görsel yüklenmesi zorunludur.");
            }

            _context.Sliders.Add(slider);
            await _context.SaveChangesAsync();
            return RedirectToAction("Add", "Slider", new { area = "Admin" });

            // Slider başarıyla eklendikten sonra, slider listesinin olduğu sayfaya yönlendir
        }
        // Silme işlemi için Action metodu
        [HttpDelete]
        public IActionResult DeleteSlider(int id)
        {
            var slider = _context.Sliders.FirstOrDefault(s => s.Id == id);
            if (slider == null)
            {
                return NotFound(new { message = "Slider bulunamadı." });
            }

            // Eğer silinecek slider mainslider ise ve aktifse, diğer aktif mainslider sayısını kontrol et
            if (slider.SliderCategory == "mainslider" && slider.IsActive)
            {
                int activeMainSliderCount = _context.Sliders
                    .Count(s => s.SliderCategory == "mainslider" && s.IsActive && s.Id != id);

                if (activeMainSliderCount < 1)
                {
                    return BadRequest(new { message = "En az 1 adet aktif Main Slider olmalıdır. Bu slider silinemez." });
                }
            }

            // Eğer silinecek slider homeslider ise ve aktifse, diğer aktif homeslider sayısını kontrol et
            if (slider.SliderCategory == "homeslider" && slider.IsActive)
            {
                int activeHomeSliderCount = _context.Sliders
                    .Count(s => s.SliderCategory == "homeslider" && s.IsActive && s.Id != id);

                if (activeHomeSliderCount < 2)
                {
                    return BadRequest(new { message = "En az 2 adet aktif Home Slider olmalıdır. Bu slider silinemez." });
                }
            }

            _context.Sliders.Remove(slider);
            _context.SaveChanges();

            return Ok(new { message = "Slider başarıyla silindi." });
        }
        // Slider Güncelleme
        [HttpPost]
        public IActionResult UpdateSlider(SliderViewModel model, IFormFile image)
        {
            var slider = _context.Sliders.FirstOrDefault(s => s.Id == model.Id);
            if (slider == null)
            {
                return NotFound(new { message = "Slider bulunamadı" });
            }

            // Slider bilgilerini güncelle
            slider.TopTitle = model.TopTitle;
            slider.Title = model.Title;
            slider.ButtonTitle = model.ButtonTitle;
            slider.ButtonUrl = model.ButtonUrl;

            // Yeni görsel yüklendiyse, eski görseli sil ve yeni görseli kaydet
            if (image != null)
            {
                // Eski görseli silme işlemi (varsa)
                var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", slider.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                // Yeni görseli kaydetme işlemi
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads); // Eğer "uploads" klasörü yoksa oluştur.
                }

                var newImageName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName); // Görsel ismini benzersiz yapmak için GUID
                var filePath = Path.Combine(uploads, newImageName);

                // Görseli kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                // Slider'da yeni görsel URL'sini güncelle
                slider.ImageUrl = "/uploads/" + newImageName;
            }

            _context.SaveChanges(); // Değişiklikleri kaydet

            return Ok(new { message = "Slider başarıyla güncellendi." });
        }

        // AdminController.cs

        // Slider'ı ID'ye göre almak için metod
        [HttpGet]
        public IActionResult GetSliderById(int id)
        {
            // ID'ye göre slider verisini veritabanından alıyoruz
            var slider = _context.Sliders.FirstOrDefault(s => s.Id == id);

            // Eğer slider bulunamazsa 404 NotFound döndürüyoruz
            if (slider == null)
            {
                return NotFound(new { message = "Slider bulunamadı." });
            }

            // Slider bilgilerini ViewModel'e aktar
            var sliderViewModel = new SliderViewModel
            {
                Id = slider.Id,
                TopTitle = slider.TopTitle,
                Title = slider.Title,
                ButtonTitle = slider.ButtonTitle,
                ButtonUrl = slider.ButtonUrl,
                ImageUrl = slider.ImageUrl // Görsel URL'sini ekliyoruz
            };

            // Slider'ı JSON formatında frontend'e gönderiyoruz
            return Ok(sliderViewModel);
        }
        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var slider = await _context.Sliders.FindAsync(id);
            if (slider == null)
            {
                return NotFound();
            }

            // Eğer homeslider aktif edilmeye çalışılıyorsa, toplam aktif sayısını kontrol et (maksimum 2)
            if (!slider.IsActive && slider.SliderCategory == "homeslider")
            {
                int activeHomeSliderCount = await _context.Sliders
                    .CountAsync(s => s.IsActive && s.SliderCategory == "homeslider");

                if (activeHomeSliderCount >= 2)
                {
                    return Json(new { success = false, message = "Maksimum 2 adet Yönlendirme Görseli yayında olabilir." });
                }
            }

            // Eğer homeslider pasif edilmeye çalışılıyorsa, en az 2 aktif kalmalı
            if (slider.IsActive && slider.SliderCategory == "homeslider")
            {
                int activeHomeSliderCount = await _context.Sliders
                    .CountAsync(s => s.IsActive && s.SliderCategory == "homeslider");

                if (activeHomeSliderCount <= 2)
                {
                    return Json(new { success = false, message = "En az 2 adet Yönlendirme Görseli yayında kalmalı." });
                }
            }

            // Eğer mainslider pasif edilmeye çalışılıyorsa, en az bir tane aktif kalmalı
            if (slider.IsActive && slider.SliderCategory == "mainslider")
            {
                int activeMainSliderCount = await _context.Sliders
                    .CountAsync(s => s.IsActive && s.SliderCategory == "mainslider");

                if (activeMainSliderCount <= 1)
                {
                    return Json(new { success = false, message = "En az 1 adet Giriş Görseli yayında kalmalı." });
                }
            }

            // Aktiflik durumunu tersine çevir
            slider.IsActive = !slider.IsActive;

            // Veritabanını güncelle
            _context.Update(slider);
            await _context.SaveChangesAsync();

            // Başarılı dönüş
            return Json(new { success = true, isActive = slider.IsActive });
        }





    }
}