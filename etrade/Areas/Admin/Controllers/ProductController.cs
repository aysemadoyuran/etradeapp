using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using etrade.Areas.Shop.Services;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]

    public class ProductController : Controller
    {
        private readonly EtradeContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly HttpClient _httpClient;
        private readonly InventoryService _inventoryService;


        public ProductController(IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, InventoryService inventoryService)
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
            _httpClient = httpClientFactory.CreateClient(); // HttpClient'i Factory'den al
            _webHostEnvironment = webHostEnvironment;
            _inventoryService = inventoryService;

        }
        public async Task<string> GenerateUniqueProductCode()
        {
            string code;
            bool exists;
            do
            {
                code = "PRD-" + Guid.NewGuid().ToString().Substring(0, 8);
                exists = await _context.Products.AnyAsync(p => p.ProductCode == code);
            } while (exists); // Eğer aynı kod varsa tekrar üret

            return code;
        }


        // Ürün oluşturma sayfası
        [HttpGet]
        public IActionResult ProductCreate()
        {
            // Kategorileri ViewBag ile gönderiyoruz
            var categories = _context.Categories
                .Select(c => new { c.Id, c.Name })
                .ToList();

            // Kategorileri SelectListItem olarak hazırlıyoruz
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

            // Başlangıçta alt kategorileri boş gönderiyoruz
            ViewBag.SubCategories = new SelectList(new List<SubCategory>(), "Id", "Name");

            return View();
        }


        // Alt kategorileri yükleme
        [HttpGet]
        public JsonResult GetSubCategories(int categoryId)
        {
            var subCategories = _context.SubCategories
                .Where(sc => sc.CategoryId == categoryId)
                .Select(sc => new { sc.Id, sc.Name })
                .ToList();

            return Json(subCategories);
        }
        //Ürünün Temel Bilgilerini Kaydet 2. Aşamaya Geç
        [HttpPost]
        public async Task<IActionResult> ProductCreate(ProductCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    CategoryId = model.CategoryId,
                    SubCategoryId = model.SubCategoryId,
                    ProductCode = await GenerateUniqueProductCode(),
                    Complete = false,
                    OriginalPrice = 0.00m
                };

                // Veritabanına kaydetme
                _context.Products.Add(product);

                try
                {
                    // Veritabanında değişiklikleri kaydet (async)
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Hata kontrolü için loglama veya hata mesajı gösterebilirsiniz.
                    Console.WriteLine($"Error: {ex.Message}");
                    // Hata durumunda modelin tekrar gönderilmesi
                    return View(model);
                }

                // Kaydedildikten sonra ürün detay sayfasına yönlendirme
                return RedirectToAction("ProductCreateStep2", "Product", new { area = "Admin", productId = product.Id });

            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
            }

            // Eğer model geçerli değilse
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }


        //Sayfaya Renkleri Yüklüyoruz.

        public IActionResult ProductCreateStep2(int productId)
        {
            // Ürünü buluyoruz
            var product = _context.Products
                                  .FirstOrDefault(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            // Ürüne ait renkleri alıyoruz
            var colors = _context.Colors.ToList();

            // Renkleri ViewBag ile gönderiyoruz
            ViewBag.Colors = JsonConvert.SerializeObject(colors);

            // Ürün Id'sini ViewModel'e ekliyoruz
            var model = new ProductCreateStep2ViewModel
            {
                ProductId = productId,
            };

            return View(model);
        }
        //2. Aşamadaki Bilgileri Kaydediyoruz.
        [HttpPost]
        public async Task<IActionResult> ProductCreateStep2(ProductCreateStep2ViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == model.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                try
                {
                    foreach (var color in model.Colors)
                    {
                        if (color.Photos != null && color.Photos.Any())
                        {
                            // **Mevcut fotoğraf sayısını kontrol et**
                            int existingImagesCount = await _context.ColorImages
                                .Where(ci => ci.ProductId == model.ProductId && ci.ColorId == color.ColorId)
                                .CountAsync();

                            // **Yeni eklenenlerle birlikte toplam fotoğraf sayısını kontrol et**
                            int newPhotosCount = color.Photos.Count;
                            int totalPhotoCount = existingImagesCount + newPhotosCount;

                            if (totalPhotoCount > 5)
                            {
                                TempData["ErrorMessage"] = $"'{product.Name}' adlı ürünün bu rengi için maksimum 5 fotoğraf eklenebilir. Mevcut: {existingImagesCount}, Eklenmek istenen: {newPhotosCount}.";

                                // **HATA DURUMUNDA ViewBag.Colors YENİDEN AYARLANIYOR!**
                                var colors = await _context.Colors.ToListAsync();
                                ViewBag.Colors = colors != null ? JsonConvert.SerializeObject(colors) : "[]";

                                return View(model);
                            }

                            foreach (var photo in color.Photos)
                            {
                                // **Dosya boyutu kontrolü (max 30MB)**
                                if (photo.Length > 30 * 1024 * 1024) // 30MB
                                {
                                    TempData["ErrorMessage"] = "Dosya boyutu maksimum 30MB olabilir.";

                                    // **ViewBag.Colors tekrar ayarlanıyor**
                                    var colors = await _context.Colors.ToListAsync();
                                    ViewBag.Colors = colors != null ? JsonConvert.SerializeObject(colors) : "[]";

                                    return View(model);
                                }

                                // **Dosya uzantısı kontrolü**
                                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                                var fileExtension = Path.GetExtension(photo.FileName).ToLower();

                                if (!allowedExtensions.Contains(fileExtension))
                                {
                                    TempData["ErrorMessage"] = "Sadece .jpg, .jpeg veya .png uzantılı dosyalar yüklenebilir.";

                                    // **ViewBag.Colors tekrar ayarlanıyor**
                                    var colors = await _context.Colors.ToListAsync();
                                    ViewBag.Colors = colors != null ? JsonConvert.SerializeObject(colors) : "[]";

                                    return View(model);
                                }

                                var fileName = Guid.NewGuid().ToString() + fileExtension;
                                var filePath = Path.Combine(uploadDir, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await photo.CopyToAsync(stream);
                                }

                                var colorImage = new ColorImage
                                {
                                    ProductId = model.ProductId,
                                    ColorId = color.ColorId,
                                    ImageUrl = "/images/" + fileName
                                };

                                _context.ColorImages.Add(colorImage);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Fotoğraflar başarıyla kaydedildi!";
                    return RedirectToAction("ProductCreateStep2", "Product", new { area = "Admin", productId = product.Id });

                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;

                    // **Hata oluştuğunda ViewBag.Colors tekrar set ediliyor**
                    var colors = await _context.Colors.ToListAsync();
                    ViewBag.Colors = colors != null ? JsonConvert.SerializeObject(colors) : "[]";

                    return View(model);
                }
            }

            // **Eğer ModelState geçersizse ViewBag.Colors tekrar set edilmeli**
            var allColors = await _context.Colors.ToListAsync();
            ViewBag.Colors = allColors != null ? JsonConvert.SerializeObject(allColors) : "[]";

            return View(model);
        }
        //Fotoğrafları Silme İşlemleri
        [HttpPost]
        public async Task<IActionResult> DeleteImage(string imageUrl)
        {
            var colorImage = await _context.ColorImages.FirstOrDefaultAsync(ci => ci.ImageUrl == imageUrl);

            if (colorImage != null)
            {
                int productId = colorImage.ProductId;  // Silinen resmin ProductId'sini al
                int colorId = colorImage.ColorId;      // Silinen resmin ColorId'sini al

                // Fotoğrafı veritabanından sil
                _context.ColorImages.Remove(colorImage);
                await _context.SaveChangesAsync();

                // **Kontrol edelim:** Bu ürün ve renk için başka fotoğraf kaldı mı?
                bool hasRemainingImages = await _context.ColorImages
                    .AnyAsync(ci => ci.ProductId == productId && ci.ColorId == colorId);

                return Json(new { success = true, hasRemainingImages });
            }

            return Json(new { success = false });
        }

        //3. Aşamaya Geçiş
        [HttpPost]
        public async Task<IActionResult> Step3(int productId)
        {
            if (productId == 0)
            {
                TempData["ErrorMessage"] = "Ürün kimliği eksik. Lütfen tekrar deneyin.";
                return RedirectToAction("ProductCreateStep2");
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var colorImageCount = await _context.ColorImages
                .Where(ci => ci.ProductId == productId)
                .CountAsync();

            if (colorImageCount == 0)
            {
                TempData["ErrorMessage"] = "Son aşamaya geçmek için en az 1 fotoğraf ve 1 renk girmeniz gerekmektedir.";
                return RedirectToAction("ProductCreateStep2", new { productId });
            }

            // Yönlendirmede productId'yi eklediğimizden emin olalım!
            return RedirectToAction("ProductCreateStep3", "Product", new { area = "Admin", productId });

        }

        //Sayfaya Ürünün Renklerini ve Bedenleri Yüklüyoruz.
        public async Task<IActionResult> ProductCreateStep3(int? productId)
        {
            if (productId == null || productId == 0)
            {
                TempData["ErrorMessage"] = "Ürün kimliği eksik. Lütfen tekrar deneyin.";
                return RedirectToAction("ProductCreateStep2");
            }

            var colorImages = await _context.ColorImages
                .Where(ci => ci.ProductId == productId)
                .Select(ci => new { ci.ColorId, ci.Color.Name })
                .Distinct()
                .ToListAsync();

            var colorList = colorImages.Select(ci => new SelectListItem
            {
                Value = ci.ColorId.ToString(),
                Text = ci.Name
            }).ToList();

            var sizes = await _context.Sizes
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();

            var sizeList = sizes.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            }).ToList();

            // Renk ve beden listelerini ViewBag ile gönderiyoruz
            ViewBag.Colors = colorList;
            ViewBag.Sizes = sizeList;

            var viewModel = new ProductCreateStep3ViewModel
            {
                ProductId = productId.Value, // Nullable'dan çıkarıyoruz
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> ProductCreateStep3(ProductCreateStep3ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.ProductId);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. ÖNCE TEMEL KAYDI OLUŞTUR (STOK = 0)
                var productVariant = new ProductVariant
                {
                    ProductId = model.ProductId,
                    ColorId = model.SelectedColorId,
                    SizeId = model.SelectedSize,
                };

                _context.ProductVariants.Add(productVariant);
                await _context.SaveChangesAsync(); // ID oluştu

                // 2. SERVİS İLE STOK GÜNCELLE (ARTIK ID MEVCUT)
                await _inventoryService.UpdateStockAsync(productVariant.Id, model.Stock, "Ürün Alışı", "Giriş", productVariant.Stock);


                // 3. TRANSACTION'ı ONAYLA
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Stok başarıyla eklendi!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Hata: " + ex.Message;
            }

            return RedirectToAction("ProductCreateStep3", new { productId = model.ProductId });
        }

        // Yardımcı metod
        private async Task PopulateDropdowns(int productId)
        {
            ViewBag.Colors = await _context.ColorImages
                .Where(ci => ci.ProductId == productId)
                .Select(ci => new SelectListItem
                {
                    Value = ci.ColorId.ToString(),
                    Text = ci.Color.Name
                })
                .Distinct()
                .ToListAsync();

            ViewBag.Sizes = await _context.Sizes
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync();
        }
        [HttpPost]
        public IActionResult CompleteProduct(int productId)
        {
            // Ürünün beden ve stok bilgilerini kontrol et
            var productVariants = _context.ProductVariants
                                          .Where(pv => pv.ProductId == productId)
                                          .ToList();

            // Eğer hiç beden ve stok kaydı yoksa
            if (!productVariants.Any(pv => pv.Stock > 0))
            {
                // Hata mesajı göster ve kullanıcıyı product create step 3 sayfasına yönlendir
                TempData["ErrorMessage"] = "En az 1 adet beden ve stok girişi yapılmalıdır.";
                return RedirectToAction("ProductCreateStep3", "Product", new { area = "Admin", productId });

            }
            else
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == productId);

                if (product != null)
                {
                    product.Complete = true;  // Ürünün complete durumunu 1 yapıyoruz
                    _context.SaveChanges();   // Değişiklikleri kaydediyoruz
                }

            }

            // Eğer bedeni ve stoğu varsa, işlemi tamamla
            // Burada ürünün tamamlanmış duruma getirilmesi, kayıt yapılması işlemleri yapılır.

            return RedirectToAction("ProductList");
        }
        public IActionResult ProductList(int page = 1, int pageSize = 15, int? categoryId = null, int? subCategoryId = null, int? colorId = null, int? sizeId = null, int? minStock = null)
        {
            // Başlangıçta tüm ürünleri al
            var query = _context.Products
                                .Include(p => p.Category)
                                .Include(p => p.SubCategory)
                                .Include(p => p.ProductVariants)
                                .ThenInclude(pv => pv.Color)
                                .Include(p => p.ProductVariants)
                                .ThenInclude(pv => pv.Size)
                                .AsQueryable();

            // Filtreleme
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (subCategoryId.HasValue)
            {
                query = query.Where(p => p.SubCategoryId == subCategoryId.Value);
            }

            if (colorId.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.ColorId == colorId.Value));
            }

            if (sizeId.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.SizeId == sizeId.Value));
            }

            if (minStock.HasValue)
            {
                query = query.Where(p => p.ProductVariants.Any(pv => pv.Stock >= minStock.Value));
            }

            // Toplam ürün sayısını al
            var totalProducts = query.Count();

            // Sayfalama işlemi
            var products = query
                            .OrderByDescending(p => p.Id) // ID'ye göre büyükten küçüğe sırala
                            .Skip((page - 1) * pageSize) // Sayfalama için atlama
                            .Take(pageSize) // Belirtilen sayıda al
                            .ToList();

            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize); // Toplam sayfa sayısını hesapla

            // Kategorileri ViewData'ya ekle
            ViewData["Categories"] = _context.Categories.ToList();
            ViewData["SubCategories"] = _context.SubCategories.ToList();
            ViewData["Colors"] = _context.Colors.ToList();
            ViewData["Sizes"] = _context.Sizes.ToList();

            // Modeli oluştur
            var viewModel = new ProductListViewModel
            {
                Products = products,
                CurrentPage = page,
                TotalPages = totalPages,
                CategoryId = categoryId,
                SubCategoryId = subCategoryId,
                ColorId = colorId,
                SizeId = sizeId,
                MinStock = minStock
            };

            return View(viewModel);
        }
        public IActionResult ConfirmDelete(int id)
        {
            var product = _context.Products
                                .Include(p => p.ProductVariants)
                                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Ürün ve versiyonlarıyla birlikte silme için kullanıcıya onay vereceğiz
            return View(product);
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _context.Products
                                .Include(p => p.ProductVariants)
                                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Ürünle ilişkili tüm ProductVariants ve diğer verileri cascade delete ile silme
            _context.Products.Remove(product);

            // Veritabanını güncelle
            _context.SaveChanges();

            // Kullanıcıya başarılı silme mesajı göstermek için yönlendirme
            TempData["SuccessMessage"] = "Ürün başarıyla silindi!";
            return RedirectToAction("ProductList", "Product", new { area = "Admin" });
        }

        [HttpGet]
        public async Task<IActionResult> ProductEdit(int id)
        {
            // Ürüne ait varyantları çek
            var productVariants = await _context.ProductVariants
                .Where(v => v.ProductId == id)
                .Include(v => v.Color)
                .Include(v => v.Size)
                .Include(v => v.ProductVariantImages)
                .ToListAsync();


            // Ürünü çek
            var product = await _context.Products
                .Where(p => p.Id == id)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            // Kategori, alt kategori, renk ve bedenleri çek
            var categories = await _context.Categories.ToListAsync();
            var subCategories = await _context.SubCategories.ToListAsync();
            var sizes = await _context.Sizes.ToListAsync();

            // Ürüne ait renkleri ve fotoğrafları ColorImages tablosundan çek
            var colorImages = await _context.ColorImages
                .Include(ci => ci.Color)
                .Where(ci => ci.ProductId == id)
                .GroupBy(ci => new { ci.ColorId, ci.Color.Name })
                .Select(group => new ColorImageViewModel
                {
                    ColorId = group.Key.ColorId,
                    ColorName = group.Key.Name,
                    ImageUrls = group.Select(ci => ci.ImageUrl).ToList()
                })
                .ToListAsync();

            // Ürün varyantları ViewModel'e aktar
            var productViewModel = new ProductViewModel
            {

                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CategoryId = product.CategoryId,
                SubCategoryId = product.SubCategoryId ?? 0,
                Variants = productVariants.Select(v => new ProductVariantViewModel
                {
                    Id = v.Id,
                    ColorId = v.ColorId,
                    ColorName = v.Color?.Name ?? "Bilinmeyen",
                    SizeId = v.SizeId,
                    SizeName = v.Size?.Name ?? "Bilinmeyen",
                    Stock = v.Stock,
                    ImageUrls = v.ProductVariantImages.Select(img => img.ImageUrl).ToList()
                }).ToList(),
                ColorImages = colorImages // ✅ Burada ColorImages'ı ProductViewModel'e ekledik!

            };

            // ViewBag ile verileri gönder
            ViewBag.Categories = categories;
            ViewBag.Colors = _context.Colors.ToList();
            ViewBag.SubCategories = subCategories;
            ViewBag.Sizes = sizes;
            Console.WriteLine($"✅ ProductEdit çağrıldı! Gelen ID: {id}");

            return View(productViewModel);
        }

        public async Task<IActionResult> GetColorImages(int productId)
        {
            if (productId == 0)
            {
                Console.WriteLine("⚠️ Hata: ProductId 0 olarak geldi!");
                return Content("Hata: Geçersiz ProductId!");
            }

            var colorImages = await _context.ColorImages
                .Include(ci => ci.Color)
                .Where(ci => ci.ProductId == productId)
                .GroupBy(ci => new { ci.ColorId, ci.Color.Name })
                .Select(group => new ColorImageViewModel
                {
                    ColorId = group.Key.ColorId,
                    ColorName = group.Key.Name,
                    ImageUrls = group.Select(ci => ci.ImageUrl).ToList(),
                    ProductId = productId // ✅ Model içinde ProductId var!
                })
                .AsNoTracking()
                .ToListAsync();


            if (colorImages == null || !colorImages.Any())
            {
                return Content("Bu Ürüne Ait Görsel Bulunmamaktadır.");
            }

            return PartialView("_ColorImagesPartial", colorImages);
        }
        [HttpPost]
        public IActionResult DeleteAllColorImages(int productId, int colorId)
        {
            try
            {
                // 1️⃣ Bu ürüne ve renge ait tüm görselleri al
                var colorImages = _context.ColorImages
                    .Where(ci => ci.ColorId == colorId && ci.ProductId == productId)
                    .ToList();

                if (colorImages.Any())
                {
                    foreach (var colorImage in colorImages)
                    {
                        // Görseli dosya sisteminden sil
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", colorImage.ImageUrl);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }

                        // Veritabanından görsel kaydını sil
                        _context.ColorImages.Remove(colorImage);
                    }
                }

                // 2️⃣ Bu ürüne ve renge ait tüm stokları sil
                var stocks = _context.ProductVariants
                    .Where(s => s.ColorId == colorId && s.ProductId == productId)
                    .ToList();

                if (stocks.Any())
                {
                    _context.ProductVariants.RemoveRange(stocks);
                }

                // 3️⃣ Değişiklikleri kaydet
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddColorAndPhotos(int productId, int colorId, IFormFile[] photos)
        {
            // Ürünü buluyoruz
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                TempData["Error"] = "Ürün bulunamadı!";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }

            if (photos == null || photos.Length == 0)
            {
                TempData["Error"] = "Lütfen fotoğraf seçin.";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }

            // 5 fotoğraf yüklenebilir kontrolü
            if (photos.Length > 5)
            {
                TempData["Error"] = "Bir renge en fazla 5 fotoğraf yükleyebilirsiniz.";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }

            // Fotoğraf uzantılarını kontrol etme (.jpeg, .jpg, .png)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            foreach (var photo in photos)
            {
                var extension = Path.GetExtension(photo.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Sadece .jpg, .jpeg, ve .png uzantılı dosyalar yükleyebilirsiniz.";
                    return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

                }

                // Fotoğraf boyutunu kontrol etme (15MB)
                if (photo.Length > 15 * 1024 * 1024) // 15 MB
                {
                    TempData["Error"] = "Fotoğraf boyutu 15MB'den fazla olamaz.";
                    return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

                }
            }

            try
            {
                // Renk ve fotoğraf ilişkisini veritabanına eklemek için kontrol
                var colorImages = await _context.ColorImages
                    .Where(ci => ci.ProductId == productId && ci.ColorId == colorId)
                    .ToListAsync();

                // Aynı renge sahip 5'ten fazla fotoğraf varsa, kullanıcıyı uyarıyoruz
                if (colorImages.Count + photos.Length > 5)
                {
                    TempData["Error"] = "Bu renge ait maksimum 5 fotoğraf yüklenebilir.";
                    return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

                }

                // Fotoğrafları kaydediyoruz
                foreach (var photo in photos)
                {
                    if (photo.Length > 0)
                    {
                        // Fotoğrafın kaydedileceği yolu belirliyoruz
                        var fileName = Path.GetFileName(photo.FileName);
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                        // Fotoğrafı kaydediyoruz
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }

                        // Renk ve fotoğraf ilişkisini veritabanına ekliyoruz
                        var newColorImage = new ColorImage
                        {
                            ProductId = productId,
                            ColorId = colorId,
                            ImageUrl = $"/images/{fileName}" // Fotoğrafın yolu
                        };

                        _context.ColorImages.Add(newColorImage);
                    }
                }

                // Veritabanına kaydediyoruz
                await _context.SaveChangesAsync();

                TempData["Success"] = "Renk ve fotoğraf başarıyla eklendi!";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }
            catch (Exception ex)
            {
                // Hata durumunda kullanıcıya bilgi veriyoruz
                TempData["Error"] = $"Bir hata oluştu: {ex.Message}";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }
        }


        [HttpPost]
        public IActionResult DeleteColorStocks(int productId, int colorId)
        {
            try
            {
                // Sadece belirtilen ürüne ve renge ait varyantları getir
                var stocksToDelete = _context.ProductVariants
                    .Where(s => s.ProductId == productId && s.ColorId == colorId)
                    .ToList();

                if (stocksToDelete.Any())
                {
                    _context.ProductVariants.RemoveRange(stocksToDelete);
                    _context.SaveChanges();
                }

                return Json(new { success = true, message = "Ürüne ait bu renkteki tüm stoklar silindi!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Hata oluştu: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetColorImageCounts(int productId)
        {
            var imageCounts = _context.ColorImages
                .Where(x => x.ProductId == productId)
                .GroupBy(x => x.ColorId)
                .ToDictionary(g => g.Key, g => g.Count());

            return Json(new { imageCounts });
        }
        [HttpPost]
        public async Task<IActionResult> Update(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == model.Id);

                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.CategoryId = model.CategoryId;
                product.SubCategoryId = model.SubCategoryId;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Değişiklikler Kaydedildi!";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = model.Id });

            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddVariant(int productId, ProductVariantViewModel model)
        {
            // Verilen productId ile ürünü buluyoruz
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Aynı ürün, renk ve beden kombinasyonu var mı kontrolü
            var existingVariant = await _context.ProductVariants
                .FirstOrDefaultAsync(pv => pv.ProductId == productId &&
                                           pv.ColorId == model.ColorId &&
                                           pv.SizeId == model.SizeId);

            if (existingVariant != null)
            {
                TempData["Error"] = "Bu renk ve beden kombinasyonu zaten eklenmiş.";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }

            // Renk geçerliliğini kontrol etme
            var colorExists = await _context.Colors.AnyAsync(c => c.Id == model.ColorId);
            if (!colorExists)
            {
                TempData["Error"] = "Geçersiz renk seçimi.";
                return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

            }

            // Yeni ProductVariant oluşturuluyor
            var productVariant = new ProductVariant
            {
                ProductId = productId,  // Burada model.ProductId yerine doğrudan productId kullanılıyor
                ColorId = model.ColorId,
                SizeId = model.SizeId,
                Stock = model.Stock
            };

            var complete = _context.Products.FirstOrDefault(p => p.Id == productId);

            if (complete != null)
            {
                product.Complete = true;  // Ürünün complete durumunu 1 yapıyoruz
                _context.SaveChanges();   // Değişiklikleri kaydediyoruz
            }
            _context.ProductVariants.Add(productVariant);
            await _context.SaveChangesAsync();
            await _inventoryService.UpdateStockAsync(productVariant.Id, productVariant.Stock, "Ürün Alışı", "Giriş", productVariant.Stock);
            TempData["Success"] = "Varyant başarıyla eklendi!";
            return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = productId });

        }

        [HttpPost]
        public async Task<IActionResult> UpdateVariants(int ProductId, int VariantId, int ColorId, int SizeId, int Stock)
        {
            // Ürünü bul
            var product = await _context.Products.FindAsync(ProductId);
            if (product == null)
            {
                return NotFound();
            }

            // İlgili varyantı bul
            var variant = await _context.ProductVariants.FindAsync(VariantId);
            if (variant == null)
            {
                return NotFound();
            }

            // Varyantı güncelle
            int oldStock = variant.Stock;

            // Stok değişimini hesapla
            int stockDifference = Stock - oldStock; // Yeni stok - Eski stok

            if (stockDifference != 0) // Eğer stok değişmişse işlem yap
            {
                string transactionType = stockDifference > 0 ? "Giriş" : "Çıkış";
                string description = stockDifference > 0 ? "Stok Güncelleme - Artış" : "Stok Güncelleme - Azalış";

                // Eğer azalış varsa değeri eksi olarak kaydet
                int adjustedStockDifference = stockDifference > 0 ? stockDifference : -Math.Abs(stockDifference);

                await _inventoryService.UpdateStockAsync(variant.Id, adjustedStockDifference, description, transactionType, Stock);
            }

            variant.ColorId = ColorId;
            variant.SizeId = SizeId;
            variant.Stock = Stock;

            // Veritabanına kaydet

            await _context.SaveChangesAsync();

            // Başarı mesajı ekle ve güncellenmiş ürünü göster
            TempData["Success"] = "Varyant başarıyla güncellendi!";
            return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = ProductId });

        }
        [HttpPost]
        public async Task<IActionResult> DeleteVariant(int VariantId)
        {
            var variant = await _context.ProductVariants.FindAsync(VariantId);
            if (variant == null)
            {
                return NotFound();
            }

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Varyant başarıyla silindi!";
            return RedirectToAction("ProductEdit", "Product", new { area = "Admin", id = variant.ProductId });

        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SetActive(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Ürünün aktiflik durumunu tersine çevir
            product.IsActive = !product.IsActive;

            _context.Update(product);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        public async Task<int> GetInactiveProductCount()
        {
            var inactiveProductsCount = await _context.Products
                .Where(p => !p.IsActive) // IsActive = false olanları filtrele
                .CountAsync(); // Sayıyı al
            return inactiveProductsCount;
        }




























































    }
}

