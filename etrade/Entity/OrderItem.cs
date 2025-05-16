using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductVariantId { get; set; }  // Ürün varyantı ID'si
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public int? DiscountId { get; set; }  // DiscountId foreign key
        public Discount Discount { get; set; }  // İndirim ile navigasyon ilişkisi

        // Navigation properties
        public virtual ProductVariant ProductVariant { get; set; }  // Ürün varyantı ilişkisi
        public virtual Order Order { get; set; }  // Sipariş ilişkisi
    }
}