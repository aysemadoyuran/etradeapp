using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class ProductVariant
    {
        public int Id { get; set; } // Primary Key

        public int ProductId { get; set; } // Ürün ID
        public Product? Product { get; set; } // Navigation Property

        public int ColorId { get; set; } // Renk ID
        public Color? Color { get; set; } // Navigation Property

        public int SizeId { get; set; } // Beden ID
        public Size? Size { get; set; } // Navigation Property

        public int Stock { get; set; } // Stok Miktarı
        public virtual ICollection<RefundedItem> RefundedItems { get; set; } = new List<RefundedItem>();

        public ICollection<ProductVariantImage> ProductVariantImages { get; set; } // Navigation Property
        public ICollection<StockMovement> StockMovements { get; set; }  // Yeni eklenen property


    }

}