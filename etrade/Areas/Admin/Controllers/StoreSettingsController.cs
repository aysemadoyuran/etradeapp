using System;
using System.Collections.Generic;
using System.Linq;
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
using Newtonsoft.Json.Linq;
using static etrade.Areas.Tenant.Controllers.StoreController;
using static etrade.Models.StoreSettingsModel;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class StoreSettingsController : Controller
    {

        private readonly EtradeContext _context;
        private readonly CachedStoreSettingsService _settingsService;

        private readonly IWebHostEnvironment _webHostEnvironment;



        public StoreSettingsController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, CachedStoreSettingsService settingsService)
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
        public async Task<IActionResult> GetStoreSetting()
        {
            // ID'yi kullanarak StoreSetting'i alıyoruz (örneğin, sadece bir tane olduğu varsayılabilir)
            var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();

            if (storeSetting == null)
            {
                return NotFound("StoreSetting bulunamadı.");
            }

            return Ok(new { isSuccess = true, data = storeSetting });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateStoreSetting([FromForm] StoreSettingUpdateModel model)
        {
            if (model == null)
            {
                return BadRequest("Güncellenen veri geçersiz.");
            }

            var existingStoreSetting = await _context.StoreSettings
                .FirstOrDefaultAsync(ss => ss.Id == 1); // ID sabit alınmış

            if (existingStoreSetting == null)
            {
                return NotFound("Güncellenecek StoreSetting bulunamadı.");
            }

            bool IsValidFileExtension(string fileName)
            {
                var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg" };
                var fileExtension = Path.GetExtension(fileName).ToLower();
                return validExtensions.Contains(fileExtension);
            }

            // LogoPath dosyası
            if (model.LogoPath != null)
            {
                if (!IsValidFileExtension(model.LogoPath.FileName))
                {
                    return BadRequest("Geçersiz dosya uzantısı. Sadece .png, .jpg, .jpeg, .svg dosyaları kabul edilir.");
                }

                var logoFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", model.LogoPath.FileName);
                using (var stream = new FileStream(logoFilePath, FileMode.Create))
                {
                    await model.LogoPath.CopyToAsync(stream);
                }
                existingStoreSetting.LogoPath = "/uploads/" + model.LogoPath.FileName;
            }

            // LogoWhitePath dosyası
            if (model.LogoWhitePath != null)
            {
                if (!IsValidFileExtension(model.LogoWhitePath.FileName))
                {
                    return BadRequest("Geçersiz dosya uzantısı. Sadece .png, .jpg, .jpeg, .svg dosyaları kabul edilir.");
                }

                var logoWhiteFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", model.LogoWhitePath.FileName);
                using (var stream = new FileStream(logoWhiteFilePath, FileMode.Create))
                {
                    await model.LogoWhitePath.CopyToAsync(stream);
                }
                existingStoreSetting.LogoWhitePath = "/uploads/" + model.LogoWhitePath.FileName;
            }

            // 🔐 API Anahtarları boş değilse şifrele ve güncelle
            if (!string.IsNullOrWhiteSpace(model.IyzicoApiKey))
            {
                existingStoreSetting.IyzicoApiKey = EncryptionHelper.Encrypt(model.IyzicoApiKey);
            }

            if (!string.IsNullOrWhiteSpace(model.IyzicoSecretKey))
            {
                existingStoreSetting.IyzicoSecretKey = EncryptionHelper.Encrypt(model.IyzicoSecretKey);
            }

            // Base URL her halükarda güncelleniyor
            existingStoreSetting.IyzicoBaseUrl = model.IyzicoBaseUrl;

            await _context.SaveChangesAsync();

            return Ok(new { isSuccess = true, message = "StoreSetting başarıyla güncellendi." });
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult EditMobileMenu()
        {
            return View();
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
        [HttpGet]
        public async Task<IActionResult> GetInvoiceInfo()
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // Debugging: JSON'u kontrol edelim
                Console.WriteLine(jsonObj.ToString());

                // InvoiceInfo'yu JSON'dan alıyoruz
                var invoiceInfo = jsonObj["InvoiceInfo"];

                // Eğer InvoiceInfo null veya boşsa hata döndürelim
                if (invoiceInfo == null || !invoiceInfo.HasValues)
                {
                    return BadRequest("InvoiceInfo verisi bulunamadı veya eksik.");
                }

                // InvoiceInfo'dan ilgili bilgileri alıyoruz
                var logoPath = invoiceInfo["logoPath"]?.ToString();
                var storeName = invoiceInfo["storeName"]?.ToString();
                var cityCountry = invoiceInfo["cityCountry"]?.ToString();
                var contactInfo = invoiceInfo["contactInfo"];

                if (contactInfo != null)
                {
                    var tel = contactInfo["tel"]?.ToString();
                    var email = contactInfo["email"]?.ToString();

                    // Veriyi kontrol etmek için loglayalım
                    Console.WriteLine($"Tel: {tel}, Email: {email}");
                }

                // Döndürülecek JSON verisini oluşturuyoruz
                var result = new
                {
                    LogoPath = logoPath,
                    StoreName = storeName,
                    CityCountry = cityCountry,
                    ContactInfo = new
                    {
                        Tel = contactInfo["tel"]?.ToString(),
                        Email = contactInfo["email"]?.ToString()
                    }
                };

                return Ok(result);  // İlgili veriyi geri döndürüyoruz
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }
        // Model
        [HttpPost]
        public async Task<IActionResult> UpdateMobileMenu([FromBody] MobileMenuModel mobileMenu)
        {
            try
            {
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null)
                    return BadRequest("Store settings not found");

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                jsonObj["MobileMenu"] = JObject.FromObject(mobileMenu);
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                _settingsService.InvalidateCache(); // Cache'i temizle
                return Ok("Mobile menu updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        public async Task<IActionResult> RemoveLinkFromMobileMenu([FromBody] LinkModel link)
        {
            try
            {
                // StoreSettings bilgisini al
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                // JSON dosya yolunu hazırla
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));
                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("JSON dosyası bulunamadı.");
                }

                // Cache'den veya dosyadan JSON'u al
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // MobileMenu linklerini al
                var mobileMenu = jsonObj["MobileMenu"] ?? new JObject();
                var links = mobileMenu["Links"] as JArray ?? new JArray();

                // Linki bul ve sil
                var linkToRemove = links.FirstOrDefault(l =>
                    string.Equals(l["Text"]?.ToString(), link.Text, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(l["Url"]?.ToString(), link.Url, StringComparison.OrdinalIgnoreCase));

                if (linkToRemove != null)
                {
                    links.Remove(linkToRemove);

                    // Değişiklikleri dosyaya kaydet
                    await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                    // Cache'i temizle
                    _settingsService.InvalidateCache();

                    return Ok(new
                    {
                        success = true,
                        message = "Link başarıyla silindi.",
                        remainingLinks = links.ToObject<List<LinkModel>>()
                    });
                }

                return NotFound(new
                {
                    success = false,
                    message = "Belirtilen link bulunamadı."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Link silme hatası",
                    error = ex.Message
                });
            }
        }
        public IActionResult EditTopBar()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateTopBarLinks([FromBody] List<LinkModel> links)
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // Gelen TopBar linklerini JSON'a yaz
                if (jsonObj["Topbar"] == null)
                {
                    jsonObj["Topbar"] = new JObject();
                }

                jsonObj["Topbar"]["Links"] = JArray.FromObject(links);

                // StoreSetting bilgisini çek
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                // Dosya yolu hazırla
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));

                // Güncellenmiş JSON'u dosyaya yaz
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                // Cache'i temizle
                _settingsService.InvalidateCache();

                return Ok(new { message = "Topbar linkleri başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kaydetme hatası: " + ex.Message });
            }
        }
        public async Task<IActionResult> RemoveLinkFromTopBar([FromBody] LinkModel link)
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // TopBar linklerini alıyoruz
                var topBar = jsonObj["Topbar"];
                var links = topBar["Links"] as JArray;

                // Silinen linki buluyoruz ve JSON'dan çıkarıyoruz
                var linkToRemove = links.FirstOrDefault(l => l["Text"]?.ToString() == link.Text && l["Url"]?.ToString() == link.Url);
                if (linkToRemove != null)
                {
                    links.Remove(linkToRemove);
                }

                // StoreSetting bilgisini çek
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));

                // Güncellenmiş JSON dosyasını kaydediyoruz
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                // Cache'i temizle
                _settingsService.InvalidateCache();

                return Ok(new { message = "Link başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Link silme hatası: " + ex.Message });
            }
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
        public IActionResult EditFooter()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateFooterSiteMapsPolicies([FromBody] StoreSettingModel storeSettings)
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // Footer'ı güncelle
                if (jsonObj["Footer"] == null)
                {
                    jsonObj["Footer"] = new JObject();
                }

                jsonObj["Footer"]["Address"] = storeSettings.Footer.Address;
                jsonObj["Footer"]["ButtonTitle"] = storeSettings.Footer.ButtonTitle;
                jsonObj["Footer"]["ButtonUrl"] = storeSettings.Footer.ButtonUrl;
                jsonObj["Footer"]["Mail"] = storeSettings.Footer.Mail;
                jsonObj["Footer"]["Phone"] = storeSettings.Footer.Phone;

                // SiteMaps'i güncelle
                if (jsonObj["SiteMaps"] == null)
                {
                    jsonObj["SiteMaps"] = new JObject();
                }

                var siteMaps = storeSettings.SiteMaps.Links.Select(link => new JObject
                {
                    ["Text"] = link.Text,
                    ["Url"] = link.Url
                }).ToList();

                jsonObj["SiteMaps"]["Links"] = JArray.FromObject(siteMaps);

                // Policies'i güncelle
                if (jsonObj["Policies"] == null)
                {
                    jsonObj["Policies"] = new JObject();
                }

                var policies = storeSettings.Policies.Links.Select(link => new JObject
                {
                    ["Text"] = link.Text,
                    ["Url"] = link.Url
                }).ToList();

                jsonObj["Policies"]["Links"] = JArray.FromObject(policies);

                // StoreSetting bilgisini çek
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));

                // Güncellenmiş JSON'u dosyaya yaz
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                // Cache'i temizle
                _settingsService.InvalidateCache();

                return Ok(new { message = "Footer, SiteMaps ve Policies başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kaydetme hatası: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemovePolicyLink([FromBody] LinkModel link)
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // Policies'den silinecek linki bul
                var policies = jsonObj["Policies"]?["Links"] as JArray;
                if (policies != null)
                {
                    var linkToRemove = policies.FirstOrDefault(l => l["Text"]?.ToString() == link.Text && l["Url"]?.ToString() == link.Url);
                    if (linkToRemove != null)
                    {
                        policies.Remove(linkToRemove);
                    }
                }

                // StoreSetting bilgisini çek
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));

                // Güncellenmiş JSON'u dosyaya yaz
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                // Cache'i temizle
                _settingsService.InvalidateCache();

                return Ok(new { message = "Policy linki başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Link silme hatası: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RemoveSiteMapLink([FromBody] LinkModel link)
        {
            try
            {
                var jsonObj = await _settingsService.GetCachedJsonAsync();

                // SiteMaps'den silinecek linki bul
                var siteMaps = jsonObj["SiteMaps"]?["Links"] as JArray;
                if (siteMaps != null)
                {
                    var linkToRemove = siteMaps.FirstOrDefault(l => l["Text"]?.ToString() == link.Text && l["Url"]?.ToString() == link.Url);
                    if (linkToRemove != null)
                    {
                        siteMaps.Remove(linkToRemove);
                    }
                }

                // StoreSetting bilgisini çek
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest("Mağaza ayarları bulunamadı.");
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));

                // Güncellenmiş JSON'u dosyaya yaz
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                // Cache'i temizle
                _settingsService.InvalidateCache();

                return Ok(new { message = "SiteMap linki başarıyla silindi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Link silme hatası: " + ex.Message });
            }
        }
        public IActionResult EditInvoiceInfo()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UpdateInvoiceInfo([FromForm] InvoiceInfoUpdateModel model)
        {
            try
            {
                // Model validation
                if (model == null)
                {
                    return BadRequest(new { message = "Geçersiz veri gönderildi." });
                }

                var jsonObj = await _settingsService.GetCachedJsonAsync() ?? new JObject();

                // InvoiceInfo section
                jsonObj["InvoiceInfo"] ??= new JObject();
                jsonObj["InvoiceInfo"]["storeName"] = model.StoreName ?? "";
                jsonObj["InvoiceInfo"]["cityCountry"] = model.CityCountry ?? "";

                // ContactInfo section
                jsonObj["InvoiceInfo"]["contactInfo"] ??= new JObject();
                jsonObj["InvoiceInfo"]["contactInfo"]["tel"] = model.Tel ?? "";
                jsonObj["InvoiceInfo"]["contactInfo"]["email"] = model.Email ?? "";

                // Handle logo upload
                if (model.LogoPath != null)
                {
                    var fileExtension = Path.GetExtension(model.LogoPath.FileName).ToLower();
                    if (fileExtension != ".png" && fileExtension != ".jpg" && fileExtension != ".jpeg")
                    {
                        return BadRequest(new { message = "Logo yalnızca .png, .jpg veya .jpeg formatında olmalıdır." });
                    }

                    if (model.LogoPath.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "Logo boyutu 2MB'den büyük olamaz." });
                    }

                    // Eski logoyu silme
                    var oldLogoPath = jsonObj["InvoiceInfo"]?["logoPath"]?.ToString();
                    if (!string.IsNullOrEmpty(oldLogoPath))
                    {
                        var oldFullPath = Path.Combine(_webHostEnvironment.WebRootPath, oldLogoPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFullPath))
                        {
                            System.IO.File.Delete(oldFullPath);
                        }
                    }

                    // Yeni logoyu kaydetme
                    var fileName = Guid.NewGuid() + fileExtension;
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);

                    Directory.CreateDirectory(Path.Combine(_webHostEnvironment.WebRootPath, "uploads"));

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.LogoPath.CopyToAsync(fileStream);
                    }

                    // Logo path güncelleme
                    jsonObj["InvoiceInfo"]["logoPath"] = "/uploads/" + fileName;
                }

                // Save to file
                var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync();
                if (storeSetting == null || string.IsNullOrEmpty(storeSetting.JsonFilePath))
                {
                    return BadRequest(new { message = "Mağaza ayarları bulunamadı." });
                }

                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, storeSetting.JsonFilePath.TrimStart('/'));
                await System.IO.File.WriteAllTextAsync(fullPath, jsonObj.ToString());

                _settingsService.InvalidateCache();

                return Ok(new
                {
                    message = "Fatura bilgileri başarıyla güncellendi.",
                    logoPath = jsonObj["InvoiceInfo"]?["logoPath"]?.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Bir hata oluştu: {ex.Message}" });
            }
        }


    }
}