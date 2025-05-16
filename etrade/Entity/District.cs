using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class District
    {
        public int IlceId { get; set; }
        [Key]
        public int Id { get; set; }
        public string SemtAdi { get; set; } = null!;
        public Ilce Ilce { get; set; }
        public ICollection<Street> Streets { get; set; }
    }
}