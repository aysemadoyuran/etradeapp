using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;

namespace etrade.Models
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string? CouponCode { get; set; }
        public decimal? TotalPrice { get; set; }  // Toplam fiyat
        public List<ItemsBasket>? BasketItems { get; set; }  // Sepetteki ürünler
        public List<Address>? UserAddresses { get; set; }  // Kullanıcının adresleri
        public int? SelectedAddressId { get; set; }  // Seçilen adresin ID'si
        public string? CardHolderName { get; set; }   // Kart sahibi adı
        public string? CardNumber { get; set; }  // Kart numarası
        public string? ExpireMonth { get; set; }  // Kartın son kullanma ayı
        public string? ExpireYear { get; set; }   // Kartın son kullanma yılı
        public string? Cvc { get; set; }   // Kartın CVC (güvenlik kodu)

        // Ödeme ile ilgili ek alanlar
        public string PaymentMethod { get; set; } = null!;  // Ödeme türü (Kredi kartı, Kapıda ödeme vb.)
        public string PaymentToken { get; set; } = null!;  // Ödeme servisinden alınan token (İyzico vb.)
        public string PaymentStatus { get; set; } = null!;  // Ödeme durumu (Ödendi, Kapıda Ödeme vb.)
    }


}