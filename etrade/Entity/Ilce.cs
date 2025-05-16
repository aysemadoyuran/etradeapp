using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace etrade.Entity
{
    public  class Ilce
    {
         [Key]
        public int Id { get; set; }

        public sbyte IlId { get; set; }

        public string Ad { get; set; } = null!;
        public Il Il { get; set; } = null!; // Bağlı olduğu il
        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}
