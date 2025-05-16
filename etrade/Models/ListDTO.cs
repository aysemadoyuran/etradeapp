using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class ListDTO
    {
        // Eğer başka bir özellik eklemek istersen, buraya ekleyebilirsin.
    }
    public class CategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
    public class SubCategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }



    public class ProductDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? ProductCode { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? Description { get; set; }
        public int? CommentCount { get; set; }
        public decimal? AverageRating { get; set; }

        public List<string>? ImageUrls { get; set; }
        public List<ProductVariantDto>? Variants { get; set; }
        public CategoryDto? Category { get; set; }  // Ana kategori
        public SubCategoryDto? SubCategory { get; set; }  // Alt kategori

    }


    public class ProductVariantDto
    {
        public int SizeId { get; set; }
        public string? SizeName { get; set; } // Ürün boyutu
        public string? ColorName { get; set; } // Renk adı
        public string? ColorCode { get; set; }
        public ColorDto? Color { get; set; } // Renk bilgisi (ColorDto sınıfı ile ilişkili)
        public List<string>? ImageUrls { get; set; } // Ürün görselleri
    }

    public class VariantDto
    {
        public int Id { get; set; }
        public int ColorId { get; set; } // Renk ID'si
        public string? ColorName { get; set; } // Renk ismi
        public List<SizeDto>? Sizes { get; set; }  // Ürün boyutları (SizeDto listesi)
        public List<string>? ImageUrls { get; set; } // Ürün görselleri
        public int Stock { get; set; }  // Ürün stoğu
    }

    public class SizeDto
    {
        public int SizeId { get; set; }
        public string? SizeName { get; set; } // Boyut ismi (Örn: M, L, XL)
        public int Stock { get; set; } // Boyut stoğu
    }

    public class ColorDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } // Renk ismi (Örn: Kırmızı)
        public string? ColorCode { get; set; } // Renk kodu (Örn: #FF0000)
    }

    public class ColorImageDto
    {
        public int Id { get; set; }
        public string? Url { get; set; } // Renk görseli URL'si
    }
}
