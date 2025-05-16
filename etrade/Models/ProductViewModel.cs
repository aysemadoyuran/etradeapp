using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;

namespace etrade.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int ColorId { get; set; }  // ColorId'yi buraya ekle

        public List<ColorViewModel> Colors { get; set; } = new List<ColorViewModel>();
        public List<ProductVariantViewModel> Variants { get; set; } = new List<ProductVariantViewModel>();
        public List<ColorImageViewModel> ColorImages { get; set; } = new List<ColorImageViewModel>(); // Renk fotoğrafları
    }



    public class ProductVariantViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public int SizeId { get; set; }
        public string? SizeName { get; set; }
        public int Stock { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>(); // Fotoğraf URL'leri
    }

    public class ColorViewModel
    {
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public List<IFormFile> Photos { get; set; } = new List<IFormFile>(); // Fotoğraf yükleme için
    }


    public class ColorImageViewModel
    {
        public int ColorId { get; set; }
        public string? ColorName { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>(); // Fotoğraf URL'leri
        public int ProductId { get; set; } // Ürün ID
    }

    public class SizeViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
    public class ProductListeViewModel
    {
        public List<Product>? Products { get; set; } // Filtrelenmiş ürünler
        public int? CategoryId { get; set; } // Kategori ID
        public int? SubCategoryId { get; set; } // Alt Kategori ID
        public int? ColorId { get; set; } // Renk ID
        public int? SizeId { get; set; } // Beden ID
        public int? MinStock { get; set; } // Min Stok
    }






}