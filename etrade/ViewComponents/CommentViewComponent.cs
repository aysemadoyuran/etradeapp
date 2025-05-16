using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class CommentListViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public CommentListViewComponent(EtradeContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(string status)
        {
            var userId = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Yorumlar ile ilişkili ürün bilgisini de dahil etmek için Include kullanıyoruz
            var comments = _context.Comments
                                   .Where(c => c.UserId == userId && c.CommentStatus == status)
                                   .Include(c => c.Product)  // Product tablosunu dahil ediyoruz
                                   .OrderByDescending(c => c.Id)  // Yorumları son eklenenden ilk eklenene doğru sıralıyoruz
                                   .ToList();

            // Burada comments'i debug edebilirsiniz
            if (comments.Count == 0)
            {
                Console.WriteLine("Hiç yorum bulunamadı!");
            }

            return View(comments);
        }
    }
}