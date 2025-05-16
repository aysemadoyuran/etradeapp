using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Category
    {
        public int Id { get; set; } // Primary Key
        public string? Name { get; set; } // Ana Kategori Adı (Dış Giyim, Üst Giyim vb.)
        public string? ImageUrl { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<SubCategory>? SubCategories { get; set; } // Alt Kategoriler ile ilişki
        public ICollection<DiscountCategory> DiscountCategories { get; set; }

        public Category()
        {
            DiscountCategories = new HashSet<DiscountCategory>();
        }


    }

}