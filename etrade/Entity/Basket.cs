using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
public class Basket
{
    public int Id { get; set; } // Sepet ID
    public string UserId { get; set; } // Kullanıcı ID (AspNetUser ID'si)
    public AppUser User { get; set; } // Kullanıcı İlişkisi
    public bool IsActive { get; set; } // Sepet aktiflik durumu
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Sepet oluşturulma tarihi
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Sepet güncellenme tarihi

    public ICollection<ItemsBasket> ItemsBaskets { get; set; } // Sepetteki ürünler
}
}