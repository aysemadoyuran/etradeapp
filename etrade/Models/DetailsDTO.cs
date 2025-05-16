using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class DetailsDTO
    {

    }
    public class ProductDetailDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ProductCode { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? OriginalPrice { get; set; }

        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public List<ImageDTO>? Images { get; set; }
        public List<ColorVariantDTO>? Variants { get; set; }
    }

    public class ImageDTO
    {
        public string? ImageUrl { get; set; }
    }

    public class ColorVariantDTO
    {
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public string? ColorCode { get; set; }
        public bool IsOutOfStock { get; set; }  // Stok durumu

        public List<SizeDTO>? Sizes { get; set; }
    }

    public class SizeDTO
    {
        public int SizeId { get; set; }
        public string SizeName { get; set; }
        public bool IsOutOfStock { get; set; } // Stok durumu
    }

}