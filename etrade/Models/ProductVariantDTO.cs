using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class ProductVariantDTO
    {
        public int ProductVariantId { get; set; }
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public int SizeId { get; set; }
        public string? SizeName { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
    }
}