using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]

    public class StoreController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;
        private readonly SeedDataService _seedDataService;
        private readonly IWebHostEnvironment _hostingEnvironment;


        public StoreController(
            ILogger<HomeController> logger,
            TenantContext tenantContext,
            TenantService tenantService,
            SeedDataService seedDataService,
            IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
            _seedDataService = seedDataService;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Add()
        {
            return View();
        }

        public static class EncryptionHelper
        {
            private static readonly string key = "SUPERGIZLIKELIME1234567890123456"; // 32 karakter (256 bit) - Bu sabit anahtar, daha güvenli bir sistemde değiştirilmelidir.

            public static string Encrypt(string plainText)
            {
                if (string.IsNullOrEmpty(plainText))
                    return plainText;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = GetFixedLengthKey(key); // Anahtarı sabit tutuyoruz, ancak burada bir güvenlik açığı olabilir.
                    aes.GenerateIV(); // Her şifrelemede farklı bir IV oluşturuluyor.
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }
                        }

                        var encryptedData = memoryStream.ToArray();
                        var ivBase64 = Convert.ToBase64String(aes.IV);
                        var encryptedBase64 = Convert.ToBase64String(encryptedData);

                        return $"{ivBase64}:{encryptedBase64}"; // IV ve şifreli metin birlikte saklanacak.
                    }
                }
            }

            public static string Decrypt(string cipherText)
            {
                if (string.IsNullOrEmpty(cipherText))
                    return cipherText;

                try
                {
                    var parts = cipherText.Split(':');
                    if (parts.Length != 2)
                        throw new CryptographicException("Geçersiz şifreli metin formatı.");

                    var ivBase64 = parts[0];
                    var encryptedBase64 = parts[1];

                    byte[] iv = Convert.FromBase64String(ivBase64);
                    byte[] encryptedData = Convert.FromBase64String(encryptedBase64);

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = GetFixedLengthKey(key);
                        aes.IV = iv;
                        aes.Padding = PaddingMode.PKCS7;

                        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                        using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                using (StreamReader streamReader = new StreamReader(cryptoStream))
                                {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                catch (FormatException ex)
                {
                    throw new CryptographicException("Şifre çözme hatası, Base64 formatı hatalı.", ex);
                }
                catch (CryptographicException ex)
                {
                    throw new CryptographicException("Şifre çözme hatası, şifreleme anahtarı veya IV uyumsuz olabilir.", ex);
                }
            }

            private static byte[] GetFixedLengthKey(string inputKey, int length = 32)
            {
                byte[] keyBytes = Encoding.UTF8.GetBytes(inputKey);
                if (keyBytes.Length < length)
                    Array.Resize(ref keyBytes, length);
                else if (keyBytes.Length > length)
                    Array.Resize(ref keyBytes, length);

                return keyBytes;
            }
        }

        public static class ConnectionStringHelper
        {
            public static string BuildConnectionString(string server, string username, string password, string dbName)
            {
                return $"Server={server};Database={dbName};User={username};Password={password};";
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] TenantCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg" };
            string logoPath = "";

            if (model.Logo != null && model.Logo.Length > 0)
            {
                var extension = Path.GetExtension(model.Logo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Sadece .png, .jpg, .jpeg veya .svg dosya formatlarına izin verilmektedir.");
                }

                var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Logo.CopyToAsync(stream);
                }

                logoPath = $"/uploads/logos/{uniqueFileName}";
            }

            string dbName = $"tenant_{Guid.NewGuid():N}".Substring(0, 8);
            string rawConnection = ConnectionStringHelper.BuildConnectionString(model.ServerName, model.Username, model.Password, model.DatabaseName);
            _logger.LogInformation("Oluşturulan connection string: " + rawConnection);

            string encryptedConn = EncryptionHelper.Encrypt(rawConnection);

            var tenant = new TenantStore
            {
                StoreName = model.StoreName,
                OwnerName = model.OwnerName,
                Email = model.Email,
                Domain = model.Domain,
                LogoUrl = logoPath,
                ConnectionString = encryptedConn,
                CreatedDate = DateTime.UtcNow
            };

            _tenantContext.TenantStores.Add(tenant);
            await _tenantContext.SaveChangesAsync();

            return Ok();
        }
        [HttpGet]
        public async Task<IActionResult> GetDomains()
        {
            // Domainleri ve id'yi TenantStore tablosundan çekiyoruz, sadece Database = false olanları alıyoruz
            var domains = await _tenantContext.TenantStores
                                              .Where(t => !t.Database)  // Database değeri false olanları getiriyoruz
                                              .OrderByDescending(t => t.Id)  // Id'ye göre sondan başa sıralıyoruz
                                              .Select(t => new { t.Id, t.Domain })
                                              .ToListAsync();

            // Eğer hiç domain yoksa TempData ile mesaj gönder
            if (!domains.Any())
            {
                TempData["Message"] = "Kurulum Bekleyen Veritabanı Yoktur";
            }

            return Json(domains);
        }
        public IActionResult DatabaseSetup()
        {
            try
            {
                throw new Exception("Bilinçli hata"); // test için
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TestController'da bir hata oluştu");
            }
            return View();
        }
        [HttpPost]
        public IActionResult UpdateTenant(TenantStore tenant, IFormFile logoFile)
        {
            // Tenant'ı veritabanından bul
            var existingTenant = _tenantContext.TenantStores.FirstOrDefault(t => t.Id == tenant.Id);

            if (existingTenant == null)
            {
                return NotFound(new { message = "Tenant bulunamadı" });
            }

            // Yeni logo dosyası varsa, onu yükle
            if (logoFile != null && logoFile.Length > 0)
            {
                // Dosya adını belirleyin, örneğin tenant'ın id'si ile dosya adını birleştirebilirsiniz
                var fileName = $"{tenant.Id}_{Path.GetFileName(logoFile.FileName)}";
                var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", fileName);

                // Dosyayı kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    logoFile.CopyTo(stream);
                }

                // Yüklenen dosyanın URL'sini tenant'ın logo URL'sine kaydedin
                existingTenant.LogoUrl = "/uploads/" + fileName;
            }

            // Diğer tenant bilgilerini güncelle
            existingTenant.StoreName = tenant.StoreName;
            existingTenant.Domain = tenant.Domain;
            existingTenant.Email = tenant.Email;
            existingTenant.OwnerName = tenant.OwnerName;

            // Veritabanına kaydet
            _tenantContext.SaveChanges();

            // Başarılı yanıt dön
            return Ok(new { message = "Tenant başarıyla güncellendi." });
        }
        [HttpPost]
        public async Task<IActionResult> StartDatabaseSetup(int tenantId)
        {
            try
            {
                // Veritabanı oluşturma işlemi
                await _tenantService.CreateTenantDatabaseAsync(tenantId);

                // Seed verisi ekleme işlemi
                await _seedDataService.SeedAllAsync(tenantId);

                // TenantStore tablosundaki ilgili kaydın Database değerini true olarak güncelle
                var tenant = await _tenantContext.TenantStores.FirstOrDefaultAsync(t => t.Id == tenantId);
                if (tenant != null)
                {
                    tenant.Database = true;  // Veritabanı oluşturulmuş olarak işaretliyoruz
                    await _tenantContext.SaveChangesAsync();  // Değişiklikleri kaydediyoruz
                }

                // Başarılı işlem mesajı
                return Content($"Tenant ID {tenantId} için veritabanı başarıyla oluşturuldu.");
            }
            catch (Exception ex)
            {
                // Hata mesajı
                return Content($"Hata oluştu: {ex.Message}");
            }
        }
        public async Task<IActionResult> GetTenantList()
        {
            var tenantStores = await _tenantContext.TenantStores
                .Select(t => new
                {
                    t.Id,
                    t.StoreName,
                    t.Domain,
                    ConnectionStringShort = t.ConnectionString.Substring(0, 20) + "..."
                })
                .ToListAsync();

            return Json(tenantStores);
        }
        public IActionResult GetTenant(int id)
        {
            var tenant = _tenantContext.TenantStores.FirstOrDefault(t => t.Id == id);

            if (tenant == null)
            {
                return NotFound(new { message = "Tenant bulunamadı" });
            }

            // Tenant bilgilerini döndür
            return Json(new
            {
                tenant.Id,
                tenant.StoreName,
                tenant.Domain,
                tenant.Email,
                tenant.LogoUrl,
                tenant.OwnerName
            });
        }
        public IActionResult List()
        {
            return View();
        }
        [HttpPost]
        public IActionResult UpdateDbConnection([FromBody] DbConnectionModels model)
        {
            try
            {
                // Model validasyonu
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Geçersiz veri",
                        errors = ModelState.Values.SelectMany(v => v.Errors)
                    });
                }

                var tenant = _tenantContext.TenantStores.FirstOrDefault(t => t.Id == model.TenantId);
                if (tenant == null)
                {
                    return NotFound(new { message = "Tenant bulunamadı" });
                }

                // Bağlantı dizesini oluştur
                var connectionString = ConnectionStringHelper.BuildConnectionString(
                    model.ServerName,
                    model.Username,
                    model.Password,
                    model.DatabaseName
                );

                tenant.ConnectionString = EncryptionHelper.Encrypt(connectionString);
                tenant.Database = !model.CreateNewDb; // Yeni DB oluşturuluyorsa aktif değil

                _tenantContext.SaveChanges();

                return Ok(new
                {
                    message = "Veritabanı bağlantısı başarıyla güncellendi",
                    connectionStringShort = connectionString.Length > 50
                        ? connectionString.Substring(0, 50) + "..."
                        : connectionString
                });
            }
            catch (Exception ex)
            {
                // Hata detayını logla
                _logger.LogError(ex, "Veritabanı bağlantısı güncellenirken hata");

                return StatusCode(500, new
                {
                    message = "Bir hata oluştu",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        public class DbConnectionModels
        {
            public int TenantId { get; set; }
            public string ServerName { get; set; }
            public string DatabaseName { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool CreateNewDb { get; set; }
        }
    }
}