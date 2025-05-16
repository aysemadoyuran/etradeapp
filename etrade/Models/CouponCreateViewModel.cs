using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;

namespace etrade.Models
{
    public class CouponCreateViewModel
    {
        public int? CouponId { get; set; }
        [Required(ErrorMessage = "Kupon kodu zorunludur")]
        [StringLength(20, ErrorMessage = "Kupon kodu en fazla 20 karakter olabilir")]
        public string Code { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "İndirim miktarı zorunludur")]
        [Range(0.01, 100000, ErrorMessage = "İndirim miktarı 0.01 ile 100.000 arasında olmalıdır")]
        public decimal DiscountValue { get; set; }

        [Range(0, 100000, ErrorMessage = "Minimum sipariş tutarı 0 ile 100.000 arasında olmalıdır")]
        public decimal? MinimumOrderAmount { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi zorunludur")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi zorunludur")]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Maksimum kullanım sayısı zorunludur")]
        [Range(1, 1000000, ErrorMessage = "Maksimum kullanım sayısı 1 ile 1.000.000 arasında olmalıdır")]
        public int MaxUsageCount { get; set; }

        [Required(ErrorMessage = "Kupon kategorisi zorunludur")]
        public string CouponCategory { get; set; } = null!;
    }
}