using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class CancellationRequest
    {
        public int Id { get; set; }
        public int LicenseId { get; set; }
        public License License { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } // Kullanıcı iptal talebi açıklaması

        public bool IsApproved { get; set; } = false;
        public DateTime? ApprovalDate { get; set; } // Ne zaman onaylandı

        public bool IsCompleted { get; set; } = false; // Kullanıcı "Verilerimi teslim aldım" dedi mi?
        public DateTime? CompletionDate { get; set; }
    }
}