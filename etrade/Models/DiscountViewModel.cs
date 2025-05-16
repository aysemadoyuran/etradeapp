using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class DiscountViewModel
    {
        public int? DiscountId { get; set; }
        public string DiscountName { get; set; }  // İndirim adı
        public string DiscountType { get; set; }  // Enum string olarak gelecek (Percentage, Fixed)
        public decimal Value { get; set; }  // İndirim değeri
        public string ConditionType { get; set; }  // Enum string olarak gelecek (Product, Category)
        public List<int> ConditionValues { get; set; }  // Seçilen ürün veya kategori ID'leri
        public DateTime StartDateTime { get; set; }  // Başlangıç tarihi
        public DateTime EndDateTime { get; set; }  // Bitiş tarihi
        public bool IsActive { get; set; }  // İndirim aktif mi?
        public List<int> SelectedIds { get; set; }  // Kategori ya da ürün ID'lerini tutacak liste

    }
}