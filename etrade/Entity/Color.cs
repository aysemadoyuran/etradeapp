using System;
using System.Collections.Generic;

namespace etrade.Entity
{
    public class Color
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ColorCode { get; set; }

        // Renk ile ilişkilendirilmiş ProductVariant'lar
        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

        // Renk ile ilişkilendirilmiş ColorImage'ler
        public ICollection<ColorImage> ColorImages { get; set; } = new List<ColorImage>();
    }
}
