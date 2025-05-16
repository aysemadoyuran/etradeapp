using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class Street
    {
        [Key]
        public int Id { get; set; }
        public int SemtId { get; set; }
        public string MahalleAdi { get; set; } = null!;
        public District District { get; set; }

    }
}