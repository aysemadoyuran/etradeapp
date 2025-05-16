using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class DiscountProduct
    {
        [Key]
        public int Id { get; set; }
        public int DiscountId { get; set; }
        public Discount Discount { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
    }
}