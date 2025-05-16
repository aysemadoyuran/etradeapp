using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class Comment
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int ProductId { get; set; }
        public string Text { get; set; } = null!;
        public string? ImageUrl { get; set; } // Yorum fotoğrafı URL'si
        public string? ImageUrl2 { get; set; } // Opsiyonel 2. fotoğraf

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Rating { get; set; }  // 1-5 arasında bir değerlendirme puanı
        public string? CommentStatus { get; set; } 
        public bool IsActive { get; set; } 

        public virtual AppUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

    }
}