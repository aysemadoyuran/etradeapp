using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Models
{
    public class UserManagementViewModel
    {
        public List<AppUser> Admins { get; set; }
        public List<AppUser> Editors { get; set; }
    }

    public class UserCreateModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; }
        public string FullName { get; set; }  // FullName alanı eklendi

    }
    public class UserRoleViewModel
    {
        public AppUser User { get; set; }
        public string Role { get; set; }
    }
}