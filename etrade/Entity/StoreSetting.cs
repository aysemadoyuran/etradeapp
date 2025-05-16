using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class StoreSetting
    {
        public int Id { get; set; }

        // Mağazanın logosu (Logo dosya yolu ya da base64 encoded string olabilir)
        public string LogoPath { get; set; } = null!;
        public string? LogoWhitePath { get; set; }
        // Kargo ücreti
        public decimal ShippingFee { get; set; }
        public string? JsonFilePath { get; set; }

        // İyzico Kimlik Bilgileri
        public string IyzicoApiKey { get; set; } = null!;
        public string IyzicoSecretKey { get; set; } = null!;
        public string IyzicoBaseUrl { get; set; } = null!;
    }
}