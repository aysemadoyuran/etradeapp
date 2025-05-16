using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class Notification
    {
        public int Id { get; set; } // Bildirimin benzersiz ID'si
        public string? UserId { get; set; } // Kullanıcıya ait ID (null ise admin bildirimi)

        // AppUser ile ilişki
        public virtual AppUser User { get; set; } // AppUser (Kullanıcı) ile ilişki

        public string Title { get; set; } // Bildirimin başlığı
        public string Message { get; set; } // Bildirimin içeriği
        public DateTime CreatedAt { get; set; } // Bildirim oluşturulma tarihi
        public bool IsRead { get; set; } // Bildirimin okunup okunmadığını belirtir
        public bool IsGlobal { get; set; } // Eğer genel bildirimse true, kullanıcıya özelse false
        public string NotificationType { get; set; } // Bildirimin türü (örneğin: "Order", "Comment" vb.)
    }
}