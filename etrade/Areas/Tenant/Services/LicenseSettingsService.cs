using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace etrade.Areas.Tenant.Services
{
    public class LicenseSettingsService
    {
        private readonly string _settingsFilePath;

        private readonly IWebHostEnvironment _env;

        public LicenseSettingsService(IWebHostEnvironment env)
        {
            _env = env;
            _settingsFilePath = Path.Combine(_env.WebRootPath, "config", "shippingSettings.json");
        }

        // JSON dosyasındaki tüm ayarları döndüren bir method
        public ShippingSettings GetShippingSettings()
        {
            if (!File.Exists(_settingsFilePath))
                return null;

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ShippingSettings>(json);
            return settings;
        }

        // Yeni ayarları JSON dosyasına yazan bir method
        public void UpdateShippingSettings(ShippingSettings newSettings)
        {
            var json = JsonSerializer.Serialize(newSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }

        // ShippingSettings sınıfını public yaparak dışarıdan erişilebilir hale getirelim
        public class ShippingSettings
        {
            public decimal StartLicense { get; set; }  // StartLicense alanı
            public decimal License { get; set; }
            public decimal KDV { get; set; }     // License alanı
            public decimal Freeze { get; set; }     // License alanı

        }
    }
}