using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }
        public int OrderId { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentMethodType { get; set; } // Ödeme türü (Örneğin: Kredi Kartı, PayPal vb.)
        public string? PaymentToken { get; set; } // İyzico vb. ödeme servisinden alınan token

        // Navigation property
        public virtual Order Order { get; set; }=null!;
    }
}