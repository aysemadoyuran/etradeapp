using System;
using System.Collections.Generic;

namespace etrade.Entity
{
    public class StockMovement
    {
        public int Id { get; set; } // Primary Key
        public int ProductVariantId { get; set; } // ProductVariant ID
        public ProductVariant? ProductVariant { get; set; } // Navigation Property

        public int Quantity { get; set; } // Miktar (Pozitif değer giriş, negatif değer çıkış için)
        public DateTime Date { get; set; } = DateTime.UtcNow; // Hareket Tarihi
        public string? MovementType { get; set; } // Hareket Türü ("in" veya "out")
        public string? Description { get; set; } // Hareket açıklaması (opsiyonel, örn. sipariş tamamlandı, stok ekle, vb.)
        public int CurrentStock { get; set; }  // İşlem sonrası mevcut stok

    }
}
