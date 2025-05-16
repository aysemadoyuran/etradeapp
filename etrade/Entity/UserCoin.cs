using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;

namespace etrade.Entity
{
    public class UserCoin
    {
        public int Id { get; set; }
        public string UserId { get; set; }=null!;
        public decimal Coin { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public AppUser User { get; set; }=null!;
    }
}