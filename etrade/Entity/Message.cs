using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Message
    {
        public int Id { get; set; }
        public string FromConnectionId { get; set; }
        public string ToConnectionId { get; set; }
        public string FromUserId { get; set; } // EKLENDİ
        public string? ToUserId { get; set; }   // EKLENDİ

        public string Tenant { get; set; } // hangi site?
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}