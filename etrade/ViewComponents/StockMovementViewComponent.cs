using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace etrade.ViewComponents
{
    public class StockMovementViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public StockMovementViewComponent(EtradeContext context)
        {
            _context = context;
        }
        public IViewComponentResult Invoke(string filter = "daily", int page = 1)
        {
            var query = _context.StockMovements.AsQueryable();

            // Filtreleme işlemi
            if (filter == "daily")
            {
                query = query.Where(sm => sm.Date >= DateTime.Today);
            }
            else if (filter == "weekly")
            {
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                query = query.Where(sm => sm.Date >= startOfWeek);
            }
            else if (filter == "yearly")
            {
                var startOfYear = new DateTime(DateTime.Today.Year, 1, 1);
                query = query.Where(sm => sm.Date >= startOfYear);
            }

            // Sayfalama
            int recordsPerPage = 10;
            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / recordsPerPage);
            var stockMovements = query.Skip((page - 1) * recordsPerPage).Take(recordsPerPage).ToList();

            var model = new StockMovementViewModel
            {
                StockMovements = stockMovements,
                TotalPages = totalPages,
                CurrentPage = page,
                Filter = filter,
                FilterOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "daily", Text = "Günlük" },
                new SelectListItem { Value = "weekly", Text = "Haftalık" },
                new SelectListItem { Value = "yearly", Text = "Yıllık" }
            }
            };

            return View(model);

        }
    }
}