using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace etrade.Models
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public IFormFile? ImageFile { get; set; }
        public string? ImageUrl { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
    }
    public class SubCategoryViewModel
    {
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<Category> Categories { get; set; }=null!;
        public List<SubCategory> SubCategories { get; set; }=null!;
    }





}