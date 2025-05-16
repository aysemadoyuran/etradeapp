using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal TotalPrice { get; set; }
        public int? ShippingAddressId { get; set; }
        public string? OrderStatus { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? UpdateDate { get; set; }

        // Navigation properties
        public virtual Address ShippingAddress { get; set; }
        public virtual AppUser User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }  // Burada ödeme yöntemini tanımlıyoruz
        public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
        public string OrderCode { get; set; }=null!;



    }
}