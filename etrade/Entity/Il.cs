using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace etrade.Entity
{
    public class Il
    {
        [Key]
        public sbyte Id { get; set; }
        public string Ad { get; set; } = null!;
        public ICollection<Ilce> Ilceler { get; set; } = new List<Ilce>();

    }

}

