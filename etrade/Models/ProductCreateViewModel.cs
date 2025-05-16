using System.ComponentModel.DataAnnotations;
using etrade.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace etrade.Models
{
    public class ProductCreateViewModel
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public decimal Price { get; set; }

    }
    public class ProductCreateStep2ViewModel
    {
        public int ProductId { get; set; }
        public List<ColorPhotoViewModel> Colors { get; set; } = new List<ColorPhotoViewModel>();
    }

    public class ColorPhotoViewModel
    {
        public int ColorId { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }
    public class ProductCreateStep3ViewModel
    {
        public int ProductId { get; set; }
        public int SelectedColorId { get; set; }
        public int SelectedSize { get; set; }
        public int Stock { get; set; }
    }
    public class ProductListViewModel
    {
        public List<Product>? Products { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // Filtreleme için eklenen alanlar
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? ColorId { get; set; }
        public int? SizeId { get; set; }
        public int? MinStock { get; set; }
    }







}
