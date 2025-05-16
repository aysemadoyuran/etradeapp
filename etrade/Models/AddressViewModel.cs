using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class AddressViewModel
    {
        public int? SelectedAddressId { get; set; } // Kullanıcının seçtiği adres ID'si

        public int? Id { get; set; }
        public string NameSurname { get; set; }

        public string AddressTitle { get; set; }

        public sbyte CityId { get; set; }
        public int DistrictId { get; set; }
        public int NeighborhoodId { get; set; }
        public int VillageId { get; set; }
        public string PhoneNumber { get; set; }
        public string AddressDetail { get; set; }
    }
}