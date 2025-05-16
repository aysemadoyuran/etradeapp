using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class City
    {
        [Key]
        public sbyte Id { get; set; }
        public string Ad { get; set; } = null!;
        public ICollection<TenantCustomer> TenantCustomers { get; set; }

    }
}