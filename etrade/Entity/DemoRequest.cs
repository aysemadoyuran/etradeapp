using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class DemoRequest
    {
        public int Id { get; set; }

        // İlişki
        public int TenantCustomerId { get; set; }
        public TenantCustomer TenantCustomer { get; set; }

        // Demo Talep Bilgileri
        public DateTime RequestDate { get; set; }
        public string RequestNote { get; set; }
        public string RequestStatus { get; set; }

        public int DemoDays { get; set; } = 7;
        public DateTime? DemoStartDate { get; set; }
        public DateTime? DemoEndDate { get; set; }

        // Yönetimsel
        public string? ApprovedByAdminId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public bool IsActive { get; set; }
    }
}