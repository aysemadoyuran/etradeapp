using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Product
    {
        public int Id { get; set; } // Primary Key
        public string? Name { get; set; } // Ürün Adı
        public string? Description { get; set; } // Açıklama
        public decimal Price { get; set; } // Fiyat
        public int CategoryId { get; set; } // Kategori ID
        public Category Category { get; set; } = null!;
        // Kategori İlişkisi
        public int? SubCategoryId { get; set; } // Alt Kategori ID (Opsiyonel)
        public SubCategory? SubCategory { get; set; }

        // Renk + Beden + Stok
        public ICollection<ProductVariant>? ProductVariants { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? ProductCode { get; set; } // Kullanıcıya gösterilecek benzersiz ürün kodu
        public bool Complete { get; set; } = false; // Varsayılan olarak 0 (tamamlanmamış)
        public ICollection<ColorImage> ColorImages { get; set; } = new List<ColorImage>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public int CommentCount { get; set; } = 0;  // Yorum sayısı
        public decimal AverageRating { get; set; } = 0;  // Ortalama puan
        public decimal? DiscountedPrice { get; set; }
        public decimal? OriginalPrice { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();  // Yorumlar
        public ICollection<DiscountProduct> DiscountProducts { get; set; }
        public int? DiscountId { get; set; }
        public Discount Discount { get; set; }

        public Product()
        {
            DiscountProducts = new HashSet<DiscountProduct>();
        }


        // Resimler

    }


}