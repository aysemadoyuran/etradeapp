using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class TenantStore
    {
        public int Id { get; set; }
        public string StoreName { get; set; } // Mağaza adı
        public string OwnerName { get; set; } // Mağaza sahibi
        public string Email { get; set; }
        public string ConnectionString { get; set; } // Oluşturulan mağaza veritabanının bağlantısı
        public string Domain { get; set; } // İleride kullanılmak üzere
        public string LogoUrl { get; set; }
        public bool Database { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? LicenseId { get; set; }  // Hangi lisansa tabi
        public License License { get; set; }
    }
}