using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Coupon
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = null!;  // Kupon kodu
        public string? Description { get; set; }  // Kupon açıklaması
        public decimal DiscountValue { get; set; }  // İndirim miktarı (tutar bazlı)
        public decimal? MinimumOrderAmount { get; set; }  // Minimum sipariş tutarı
        public DateTime StartDate { get; set; }  // Kuponun başlangıç tarihi
        public DateTime EndDate { get; set; }  // Kuponun bitiş tarihi
        public bool IsActive { get; set; }  // Kupon aktif mi?
        public int MaxUsageCount { get; set; }  // Toplam kullanım limiti
        public int CurrentUsageCount { get; set; }  // Şu ana kadar kullanılan kupon sayısı
        public string? CouponCategory { get; set; }  // Kupon kategorisi

        // Kampanyanın geçerli olması için alınması gereken minimum ürün sayısı
        public int? MinimumProductCount { get; set; }

        // Coupon Kullanım Takibi
        public ICollection<CouponUsage>? CouponUsages { get; set; }
    }
}