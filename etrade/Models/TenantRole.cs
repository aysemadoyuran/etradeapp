using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace etrade.Models
{
    public class TenantRole : IdentityRole
    {
        public TenantRole() : base() { }

        public TenantRole(string roleName) : base(roleName)
        {
        }
    }
}