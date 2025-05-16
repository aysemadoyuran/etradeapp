using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class RefundRequest
    {
        [Key]
        public int RefundRequestId { get; set; } // Primary key
        public int OrderId { get; set; } // İade talebine ait siparişin ID'si
        public int PaymentMethodId { get; set; } // PaymentMethod ile ilişki için id
        public string? Iban { get; set; }
        public string? fullName { get; set; }
        public decimal TotalPrice { get; set; } // İade edilen ürünlerin toplam tutarı
        public string RefundStatus { get; set; } = null!; // İade durumu (Pending, Completed, etc.)
        public DateTime RefundRequestDate { get; set; } // İade talebinin oluşturulma tarihi
        public bool IsValid { get; set; } // İade talebinin geçerliliği (7 gün içinde mi?)

        // İade edilen ürünlerle olan ilişki
        public PaymentMethod? PaymentMethod { get; set; }
        public Order Order { get; set; }

        public List<RefundedItem> RefundedItems { get; set; }
        public string RefundCode { get; set; } = null!;

    }
}