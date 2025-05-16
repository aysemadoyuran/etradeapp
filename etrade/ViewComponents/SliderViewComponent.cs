using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class SliderViewComponent : ViewComponent
    {

        private readonly EtradeContext _context;

        public SliderViewComponent(EtradeContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Yalnızca aktif ve SliderCategory'si "mainslider" olan slider'ları al
            var sliders = await _context.Sliders
                                         .Where(s => s.IsActive == true && s.SliderCategory == "mainslider")
                                         .ToListAsync();

            return View(sliders);
        }


    }
}