using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class HomeCommentViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public HomeCommentViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var reviews = await _context.Comments
                .Where(r => r.CommentStatus == "Onaylandı" && (r.Rating == 4 || r.Rating == 5))
                .Include(r => r.Product)
                .ThenInclude(p => p.ColorImages)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(reviews);
        }
    }
}