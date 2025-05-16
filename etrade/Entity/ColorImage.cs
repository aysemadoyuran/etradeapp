using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class ColorImage
    {
        public int Id { get; set; } // Fotoğraf ID
        public int ProductId { get; set; } // Ürün ID (Yeni eklenen alan)
        public Product Product { get; set; }=null!; // Navigation Property (Ürün ile ilişki)

        public int ColorId { get; set; } // Renk ID
        public Color Color { get; set; } =null!;// Navigation Property

        public string? ImageUrl { get; set; } // Fotoğraf URL'si (Fiziksel dosya yolu)
    }
}
