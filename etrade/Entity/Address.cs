using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class Address
    {
        public int Id { get; set; }
        public string? NameSurname {get; set;}

        public string UserId { get; set; } = null!; // Kullanıcı ID'si
        public AppUser User { get; set; } = null!;

        public sbyte IlId { get; set; } // İl ID'si (sbyte olarak değiştirildi)
        public Il Il { get; set; } = null!;

        public int IlceId { get; set; }
        public Ilce Ilce { get; set; } = null!;

        public int SemtId { get; set; }
        public District District { get; set; } = null!;

        public int MahalleId { get; set; }
        public Street Street { get; set; } = null!;

        public string? AcikAdres { get; set; }
        public string? Telefon { get; set; }
        public string? Title { get; set; }
    }
}