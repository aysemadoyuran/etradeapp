using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]


    public class CategoryController : Controller
    {
        private readonly EtradeContext _context;

        public CategoryController(IHttpContextAccessor httpContextAccessor)
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

        public IActionResult Add()
        {
            var model = new CategoryViewModel
            {
                Categories = _context.Categories.OrderByDescending(m => m.Id).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Add(CategoryViewModel model, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                // ModelState hatalarını konsola yazdır
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Hata: {error.ErrorMessage}");
                }
                TempData["Error"] = "Girdiğiniz bilgiler geçerli değil. Lütfen tekrar kontrol edin.";
                return View(model); // Hatalıysa formu geri döndür
            }

            try
            {
                // Fotoğraf dosyası kontrolü
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Maksimum dosya boyutu (15 MB)
                    const int maxFileSize = 15 * 1024 * 1024; // 15 MB
                    if (imageFile.Length > maxFileSize)
                    {
                        TempData["Error"] = "Dosya boyutu 15 MB'yi geçemez!";
                        return RedirectToAction("Add", "Category");
                    }

                    // Dosya uzantısı kontrolü (sadece .png, .jpeg, .jpg)
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Geçersiz dosya formatı! Yalnızca .jpg, .jpeg veya .png formatlarını yükleyebilirsiniz.";
                        return RedirectToAction("Add", "Category");
                    }

                    // Dosya adını benzersiz yapmak için GUID ekliyoruz
                    var fileName = Path.GetFileNameWithoutExtension(imageFile.FileName) + "_" + Guid.NewGuid().ToString() + fileExtension;

                    // Fotoğrafı kaydedeceğimiz dizin (wwwroot/uploads)
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);  // Klasörü oluşturuyoruz
                    }

                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Fotoğrafı kaydediyoruz
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        // Fotoğrafın yolunu model'e ekliyoruz
                        model.ImageUrl = "/uploads/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fotoğraf kaydetme hatası: {ex.Message}");
                        TempData["Error"] = "Fotoğraf kaydedilirken bir hata oluştu.";
                        return RedirectToAction("Add", "Category");
                    }
                }

                // Kategoriyi veritabanına kaydediyoruz
                var category = new Category
                {
                    Name = model.Name,
                    ImageUrl = model.ImageUrl // Fotoğraf yolunu kaydediyoruz
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Kategori başarıyla eklendi.";
                return RedirectToAction("Add", "Category"); // Listeleme sayfasına yönlendirme
            }
            catch (Exception ex)
            {
                // Genel hata mesajı
                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen tekrar deneyin.");
                Console.WriteLine($"Hata: {ex.Message}");
                return View(model);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateCategory(int id, string name, IFormFile imageFile)
        {
            // Loglama - Başlangıç
            Console.WriteLine($"Gelen Kategori ID: {id}");
            Console.WriteLine($"Gelen Kategori Adı: {name}");
            Console.WriteLine($"Gelen Fotoğraf: {imageFile?.FileName}");

            // Geçersiz ID kontrolü
            if (id <= 0)
            {
                Console.WriteLine("Geçersiz kategori ID'si!");
                return Json(new { success = false, message = "Geçersiz kategori ID'si!" });
            }

            // Kategoriyi bulma
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                Console.WriteLine("Kategori bulunamadı!");
                return Json(new { success = false, message = "Kategori bulunamadı!" });
            }

            // Fotoğraf dosyası kontrolü
            if (imageFile != null && imageFile.Length > 0)
            {
                const int maxFileSize = 15 * 1024 * 1024; // 15 MB
                if (imageFile.Length > maxFileSize)
                {
                    Console.WriteLine("Dosya boyutu 15 MB'yi geçemez!");
                    return Json(new { success = false, message = "Dosya boyutu 15 MB'yi geçemez!" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    Console.WriteLine("Geçersiz dosya formatı!");
                    return Json(new { success = false, message = "Geçersiz dosya formatı! Yalnızca .jpg, .jpeg veya .png formatlarını yükleyebilirsiniz." });
                }

                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);  // Klasörü oluşturuyoruz
                }

                var filePath = Path.Combine(uploadsPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    category.ImageUrl = "/uploads/" + fileName;  // Yeni fotoğraf URL'sini kaydediyoruz
                    Console.WriteLine($"Yeni fotoğraf başarıyla kaydedildi: {category.ImageUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fotoğraf kaydetme hatası: {ex.Message}");
                    return Json(new { success = false, message = "Fotoğraf kaydedilirken bir hata oluştu." });
                }
            }

            // Kategori ismini güncelleme
            category.Name = name;
            _context.SaveChanges();

            Console.WriteLine("Kategori başarıyla güncellendi.");
            return Json(new { success = true });
        }







        public IActionResult GetCategory(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }
            return Json(category);
        }
        [HttpPost]
        public IActionResult DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Kategori bulunamadı." });
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        public IActionResult SubCategories(int page = 1, int pageSize = 10)
        {
            var totalSubCategories = _context.SubCategories.Count(); // Alt kategori toplam sayısı
            var subCategories = _context.SubCategories
                                        .OrderByDescending(sc => sc.Id) // ID'ye göre büyükten küçüğe sıralama
                                        .Skip((page - 1) * pageSize) // Sayfa başı offset
                                        .Take(pageSize) // Alınacak veri miktarı
                                        .Include(sc => sc.Category) // Kategorileri de dahil et
                                        .ToList();

            var viewModel = new SubCategoryViewModel
            {
                SubCategories = subCategories,
                TotalItems = totalSubCategories,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalSubCategories / pageSize)
            };

            var categories = _context.Categories.ToList(); // Kategorileri al
            ViewBag.Categories = new SelectList(categories, "Id", "Name"); // Kategorileri ViewBag'e aktar

            return View(viewModel);
        }


        // Alt kategori ekleme işlemi
        [HttpPost]
        public IActionResult AddSubCategories(int categoryId, string name)
        {
            if (ModelState.IsValid)
            {
                // Yeni alt kategori oluşturuluyor
                var subCategory = new SubCategory
                {
                    Name = name,
                    CategoryId = categoryId
                };

                _context.SubCategories.Add(subCategory);
                _context.SaveChanges();

                // Kategori detaylarına yönlendirme
                return RedirectToAction("SubCategories", "Category", new { area = "Admin" });
            }

            // Model geçerli değilse, kategorileri tekrar gönderiyoruz
            var categories = _context.Categories
                                    .Select(c => new { c.Id, c.Name })
                                    .ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            return View(); // Formu tekrar gösteriyoruz
        }

        // Alt Kategori Güncelleme
        [HttpPost]
        public JsonResult UpdateSubCategory(int id, string name, int categoryId)
        {
            var subCategory = _context.SubCategories.Find(id);
            if (subCategory != null)
            {
                subCategory.Name = name;
                subCategory.CategoryId = categoryId;
                _context.SaveChanges();
                return Json(new { success = true, message = "Alt kategori başarıyla güncellendi." });
            }
            return Json(new { success = false, message = "Alt kategori bulunamadı." });
        }

        // Alt Kategori Silme
        [HttpPost]
        public JsonResult DeleteSubCategory(int id)
        {
            var subCategory = _context.SubCategories.Find(id);
            if (subCategory != null)
            {
                _context.SubCategories.Remove(subCategory);
                _context.SaveChanges();
                return Json(new { success = true, message = "Alt kategori başarıyla silindi." });
            }
            return Json(new { success = false, message = "Alt kategori bulunamadı." });
        }

        // Alt Kategori Getir
        [HttpGet]
        public JsonResult GetSubCategory(int id)
        {
            var subCategory = _context.SubCategories
                .Where(sc => sc.Id == id)
                .Select(sc => new { sc.Id, sc.Name, sc.CategoryId })
                .FirstOrDefault();

            if (subCategory != null)
            {
                return Json(subCategory);
            }

            return Json(new { success = false, message = "Alt kategori bulunamadı." });
        }
        // Kategorileri Getirme
        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = _context.Categories
                .Select(c => new { c.Id, c.Name }) // Sadece ID ve Name alanlarını alıyoruz
                .ToList();

            return Json(categories);
        }

    }





}
