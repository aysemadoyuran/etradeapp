using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class ProductVariantImage
    {
        public int Id { get; set; } // Primary Key

        public int ProductVariantId { get; set; } // ProductVariant ID
        public ProductVariant? ProductVariant { get; set; } // Navigation Property

        public string? ImageUrl { get; set; } // Fotoğrafın URL'si (Fiziksel dosya yolu veya bir bağlantı)
    }
}