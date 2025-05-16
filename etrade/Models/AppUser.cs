using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;
using Microsoft.AspNetCore.Identity;

namespace etrade.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; } = true; // Varsayılan olarak aktif

        [MaxLength(10)]
        public string UserCode { get; set; }

        [MaxLength(10)]
        public string? InvitationCode { get; set; }
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Favorite> Favorites { get; set; } // Kullanıcıya ait favoriler
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();  // Yorumlar
        public ICollection<CouponUsage> CouponUsages { get; set; }  // Kullanıcıya ait kupon kullanımları
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual UserCoin UserCoin { get; set; }

        public virtual ICollection<Order> Orders { get; set; }  // Kullanıcının siparişleri





    }
}