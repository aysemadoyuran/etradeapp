using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class License
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public TenantCustomer TenantCustomer { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DurationInMonths { get; set; }

        public ICollection<LicensePayment> Payments { get; set; }
        public ICollection<TenantStore> TenantStores { get; set; } // Bir lisans birden fazla mağazaya bağlı olabilir.

        public bool IsFrozen { get; set; } = false; // Lisans donduruldu mu?
        public DateTime? FreezeDate { get; set; }   // Ne zaman donduruldu?
        public DateTime? ActiveDate { get; set; }
        public string? FrozenCode { get; set; }
        public string? LicenseType { get; set; }

        public bool IsDeleted { get; set; } = false; // Soft-delete için
        public DateTime? DeletionDate { get; set; }  // Ne zaman silindi (iptal tamamlandıysa)
        public string? FreezePaymentTransactionId { get; set; } // iyzico üzerinden alınan freeze ücreti işlem ID’si
        public ICollection<CancellationRequest> CancellationRequests { get; set; }
        public ICollection<FreezePayment> FreezePayments { get; set; }
    }
}