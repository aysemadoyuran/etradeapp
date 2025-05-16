using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class BasketViewModel
    {
        public int BasketId { get; set; }
        public List<BasketItemViewModel> ItemsBaskets { get; set; }
    }
    public class BasketItemViewModel
    {
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductImageUrl { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }

        public int ColorId { get; set; } // Seçilen renk ID'si
        public int SizeId { get; set; }  // Seçilen beden ID'si
        public decimal Price { get; set; }


        // Seçili renk ve beden isimleri
        public string SelectedColor { get; set; }
        public string SelectedSize { get; set; }
    }
}