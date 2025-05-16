using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace etrade.Models
{
    public class StockMovementViewModel
    {
        public List<StockMovement>? StockMovements { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string? Filter { get; set; }
        public IEnumerable<SelectListItem>? FilterOptions { get; set; }

    }
}