using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class RefundedItem
    {
        [Key]
        public int RefundedItemId { get; set; } // İade edilen ürünün ID'si (Primary Key)
        public int RefundRequestId { get; set; } // İade talebine ait ID (Foreign Key)
        public int ProductVariantId { get; set; } // İade edilen ürünün varyant ID'si (Foreign Key)
        public int Quantity { get; set; } // İade edilen ürünün miktarı
        public decimal TotalPrice { get; set; } // Ürünün birim fiyatı
        public string ReasonType { get; set; }=null!;

        // Foreign Key İlişkileri
        public virtual RefundRequest RefundRequest { get; set; } // İade talebiyle ilişki
        public virtual ProductVariant ProductVariant { get; set; } // İade edilen ürünle ilişki
    }
}