using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class LicensePayment
    {
        public int Id { get; set; }
        public int LicenseId { get; set; }
        public License License { get; set; }

        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        public decimal Price { get; set; }
        public bool IsPaid { get; set; }
        public string? PaymentToken { get; set; }

    }
}