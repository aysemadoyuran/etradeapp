using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    // OrderDetailsViewModel.cs
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }=null!;
        public string UserName { get; set; }
        public string OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public ShippingAddressViewModel ShippingAddress { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; }
        public string PaymentMethod { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // ShippingAddressViewModel.cs
    public class ShippingAddressViewModel
    {
        public string Address { get; set; }
        public string Phone { get; set; }
        public string NameSurname { get; set; }

    }

    // OrderItemViewModel.cs
    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public int RefundedQuantity { get; set; } // İade edilen miktar

    }
}