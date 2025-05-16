using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class TenantCustomer
    {

        public int Id { get; set; }

        // Temel Bilgiler
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // Firma Bilgileri
        public string CompanyName { get; set; }
        public string TaxNumber { get; set; }
        public string TaxOffice { get; set; }

        // Adres
        public string Address { get; set; }
        public sbyte IlId { get; set; } // Şehir ID
        public City City { get; set; }    // Navigation property
        public string ZipCode { get; set; }

        // İlişkiler
        public ICollection<License> Licenses { get; set; }
        public ICollection<DemoRequest> DemoRequests { get; set; }
        public string? UserId { get; set; } // AspNetUsers tablosundaki Id
        public TenantUser User { get; set; }  // Navigation property



        
        [NotMapped]
        public string Password { get; set; }

        [NotMapped]
        public string ConfirmPassword { get; set; }
        [NotMapped]
        public string UserName { get; set; }

    }
}