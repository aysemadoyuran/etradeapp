using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class ItemsBasket
    {
        public int Id { get; set; } // Primary Key
        public int BasketId { get; set; } // Sepet ID
        public Basket Basket { get; set; } // Navigation Property

        public int ProductVariantId { get; set; } // ProductVariant ID
        public ProductVariant ProductVariant { get; set; } // Navigation Property

        public int Quantity { get; set; } 

        public bool IsActive { get; set; } // Aktiflik durumu
        public decimal TotalPrice { get; set; }
    }
}