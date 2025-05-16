using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Entity
{
    public class DiscountCategory
    {
        [Key]
        public int Id { get; set; }
        public int DiscountId { get; set; }
        public Discount Discount { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}