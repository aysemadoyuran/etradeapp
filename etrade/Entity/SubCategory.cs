using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class SubCategory
    {
        public int Id { get; set; } // Primary Key
        public string? Name { get; set; } // Alt Kategori Adı (Trençkot, Ceket vb.)

        public int CategoryId { get; set; } // Ana Kategori ID
        public Category? Category { get; set; } // Navigation Property

        public ICollection<Product>? Products { get; set; } // Alt Kategorideki Ürünler
    }

}