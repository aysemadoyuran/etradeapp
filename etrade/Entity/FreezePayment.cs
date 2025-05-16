using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class FreezePayment
    {
        public int Id { get; set; }
        public int LicenseId { get; set; }
        public License License { get; set; }

        public decimal Price { get; set; }
        public bool IsPaid { get; set; }
        public string TransactionId { get; set; } // iyzico işlem numarası
        public DateTime PaymentDate { get; set; }
    }
}