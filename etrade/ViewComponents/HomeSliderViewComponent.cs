using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class HomeSliderViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public HomeSliderViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var sliders = await _context.Sliders
                .Where(s => s.IsActive && s.SliderCategory == "homeslider")
                .Take(2)
                .ToListAsync();

            return View(sliders);
        }
    }
}