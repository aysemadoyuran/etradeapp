using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class OrderListViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }=null!;
        public DateTime? OrderDate { get; set; }
        public string OrderDateFormatted { get; set; }  // Formatlı tarih
        public decimal TotalPrice { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string AlıcıAdSoyad { get; set; }  // Alıcı adı soyad bilgisi
        public int OrderItemCount { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string OrderStatusClass
        {
            get
            {
                return OrderStatus switch
                {
                    "Beklemede" => "text-warning",
                    "Onaylandı" => "text-primary",
                    "Hazırlanıyor" => "text-info",
                    "Kargoya Verildi" => "text-success",
                    "Teslim Edildi" => "text-muted",
                    _ => "text-secondary"
                };
            }
        }

        // OrderStatus'a göre ikon sınıfı döndürme
        public string OrderStatusIconClass
        {
            get
            {
                return OrderStatus switch
                {
                    "Beklemede" => "fa-clock",
                    "Onaylandı" => "fa-check-circle",
                    "Hazırlanıyor" => "fa-cogs",
                    "Kargoya Verildi" => "fa-shipping-fast",
                    "Teslim Edildi" => "fa-check-double",
                    "İptal Edildi"=>"fa-ban",
                    _ => "fa-ban"
                };
            }
        }
    }
}