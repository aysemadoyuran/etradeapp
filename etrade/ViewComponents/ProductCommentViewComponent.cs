using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.ViewComponents
{
    public class ProductCommentViewComponent : ViewComponent
    {
        private readonly EtradeContext _context;

        public ProductCommentViewComponent(EtradeContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            var comments = await _context.Comments
                .Where(c => c.ProductId == productId && c.CommentStatus == "Onaylandı")
                .Include(c => c.User) // Kullanıcı adını almak için
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var totalReviews = comments.Count;
            var totalStars = comments.Sum(c => c.Rating); // Toplam yıldız sayısı
            var averageRating = totalReviews > 0 ? (double)totalStars / totalReviews : 0; // Ortalama hesaplama

            // Her puan türü için kaç kişinin değerlendirme yaptığını hesaplıyoruz
            var starCounts = new int[5];
            for (int i = 1; i <= 5; i++)
            {
                starCounts[i - 1] = comments.Count(c => c.Rating == i);
            }

            // Fotoğrafları listele (ImageUrl ve ImageUrl2 boş değilse ekle)
            var images = comments
                .SelectMany(c => new[] { c.ImageUrl, c.ImageUrl2 })
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();

            var model = new CommentViewModel
            {
                Comments = comments,
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                StarCounts = starCounts,
                Images = images // Yeni eklenen fotoğraf listesi
            };

            return View(model);
        }
    }
}