using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class CouponUsage
    {
        public int Id { get; set; }  // Kupon kullanım ID'si
        public int CouponId { get; set; }  // İlgili kuponun ID'si
        public string UserId { get; set; }  // Kullanıcı ID'si
        public DateTime UsageDate { get; set; }  // Kupon kullanım tarihi
        public bool IsUsed { get; set; }  // Kuponun kullanılıp kullanılmadığı
        public Coupon Coupon { get; set; }  // İlgili kupon
        public AppUser User { get; set; }  // Kuponu kullanan kullanıcı

    }
}
