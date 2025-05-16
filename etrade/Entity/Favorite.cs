using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class Favorite
    {
        public int Id { get; set; } // Benzersiz ID
        public string UserId { get; set; } // Kullanıcıyı tanımlar
        public int ProductId { get; set; } // Favorilere eklenen ürünün ID'si

        // Navigation properties
        public virtual AppUser User { get; set; } // Kullanıcı
        public virtual Product Product { get; set; } // Ürün
    }
}