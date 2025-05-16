using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Enums;

namespace etrade.Entity
{
    public class Discount
    {
        public int Id { get; set; }
        public string? Name { get; set; } // İndirim Adı
        public DiscountType DiscountType { get; set; } // Yüzdesel mi sabit mi?
        public decimal Value { get; set; } // %30 indirimse 30, 50 TL sabitse 50
        public ConditionType ConditionType { get; set; } // Ürün mü kategori mi?
        public int ConditionValue { get; set; } // ProductId veya CategoryId

        public DateTime StartDateTime { get; set; } // İndirim başlangıç tarihi ve saati
        public DateTime EndDateTime { get; set; } // İndirim bitiş tarihi ve saati
        public int? BuyQuantity { get; set; }  // Kaç tane alırsa
        public int? FreeQuantity { get; set; }  // Kaç tane bedava verilecek
        public bool IsActive { get; set; } // İndirim aktif mi?

        public ICollection<DiscountProduct> DiscountProducts { get; set; }
        public ICollection<DiscountCategory> DiscountCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();


        public Discount()
        {
            DiscountProducts = new HashSet<DiscountProduct>();
            DiscountCategories = new HashSet<DiscountCategory>();
        }
    }
}