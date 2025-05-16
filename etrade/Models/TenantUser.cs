using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;
using Microsoft.AspNetCore.Identity;

namespace etrade.Models
{
    public class TenantUser : IdentityUser
    {
        public TenantCustomer TenantCustomer { get; set; }

    }
}