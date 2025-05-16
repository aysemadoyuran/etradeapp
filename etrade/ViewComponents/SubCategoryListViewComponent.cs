using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using etrade.Data;
using etrade.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;

namespace etrade.ViewComponents
{
    public class SubCategoryListViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public SubCategoryListViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int id)
        {
            // Ana kategoriye göre alt kategorileri grupla
            var groupedSubCategories = await _context.SubCategories
                .Include(sc => sc.Category)  // Kategori ile ilişkilendiriyoruz
                .GroupBy(sc => sc.Category.Name)  // Ana kategori adına göre grupla
                .ToListAsync();

            return View(groupedSubCategories);
        }
    }
}
