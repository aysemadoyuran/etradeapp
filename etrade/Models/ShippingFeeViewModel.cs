using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class ShippingFeeViewModel
    {

        [Required(ErrorMessage = "Kargo ücreti zorunludur")]
        [Range(0, 9999, ErrorMessage = "Geçerli bir ücret giriniz")]
        public decimal ShippingFee { get; set; }

    }
}