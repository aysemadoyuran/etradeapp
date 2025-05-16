using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models.Tenant
{
    public class TenantCreateViewModel
    {
        public string StoreName { get; set; }
        public string OwnerName { get; set; }
        public string Domain { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public IFormFile Logo { get; set; }
    }
}