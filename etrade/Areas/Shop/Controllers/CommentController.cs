using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]

    public class CommentController : Controller
    {
        private readonly EtradeContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BasketController> _logger;


        public CommentController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, ILogger<BasketController> logger)
        {
            var httpContext = httpContextAccessor.HttpContext;

            // HttpContext null kontrolü
            if (httpContext == null)
            {
                throw new InvalidOperationException("HttpContext mevcut değil. Bu, middleware'de bir sorun olduğunu gösterebilir.");
            }

            // DbContext null kontrolü
            _context = httpContext.Items["DbContext"] as EtradeContext;

            if (_context == null)
            {
                throw new Exception("DbContext bulunamadı. TenantMiddleware çalışıyor mu?");
            }
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;


        }
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var purchasedProducts = await _context.OrderItems
                .Where(oi => oi.Order.UserId == userId
                             && oi.Order.OrderStatus == "Teslim Edildi"
                             && oi.ProductVariant.Product.IsActive)
                .Include(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Include(oi => oi.ProductVariant.Product.ColorImages)
                .Include(oi => oi.ProductVariant.Product.ProductVariants)
                .OrderByDescending(oi => oi.Order.UpdateDate) // 🔽 En güncel sipariş en üstte
                .ToListAsync();

            // Ürünleri sıralı şekilde alıp tekrar etmeyecek şekilde ayıklıyoruz
            var orderedDistinctProducts = purchasedProducts
                .Select(oi => oi.ProductVariant.Product)
                .Where(p => !_context.Comments.Any(c => c.UserId == userId && c.ProductId == p.Id))
                .DistinctBy(p => p.Id) // .NET 6+ LINQ extension
                .ToList();

            return View(orderedDistinctProducts);
        }
        [HttpPost]
        public async Task<IActionResult> AddComment(int productId, int rating, string commentText,
                                            IFormFile image1, IFormFile image2)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Kullanıcı ID'sini al

            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Geçersiz puan." });
            }

            // Yorum objesini oluştur
            var comment = new Comment
            {
                UserId = userId,
                ProductId = productId,
                Rating = rating,
                Text = commentText,
                CreatedAt = DateTime.UtcNow,
                CommentStatus = "Beklemede",
                IsActive = true
            };
            Console.WriteLine($"UserId: {userId}, ProductId: {productId}, Rating: {rating}, CommentText: {commentText}, CreatedAt: {DateTime.UtcNow}");

            // Fotoğrafları kaydet
            try
            {
                if (image1 != null)
                {
                    comment.ImageUrl = await SaveImage(image1);
                }
                if (image2 != null)
                {
                    comment.ImageUrl2 = await SaveImage(image2);
                }

                // Yorum veritabanına kaydet
                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yorum başarıyla eklendi." });
            }
            catch (Exception ex)
            {
                // Hata logu ekle
                _logger.LogError(ex, "Yorum eklenirken hata oluştu.");
                return Json(new { success = false, message = "Yorum eklenirken bir hata oluştu." });
            }
        }

        // Fotoğraf kaydetme fonksiyonu
        private async Task<string> SaveImage(IFormFile file)
        {
            // Dosya ismini benzersiz yapmak için Guid kullan
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // Dosyanın kaydedileceği tam yol
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", fileName);

            // Dosyayı kaydet
            try
            {
                // uploads klasörüne dosyayı kaydediyoruz
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                // Hata logu ekle
                _logger.LogError(ex, "Dosya kaydedilirken hata oluştu.");
                throw; // Hatayı yeniden fırlat
            }

            // Resmin URL'sini döndürüyoruz, "/uploads/{fileName}"
            return "/uploads/" + fileName;
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return Json(new { success = false, message = "Yorum bulunamadı!" });
            }

            // Yorum onaylıysa ve kullanıcı para puanı kazandıysa, bu puanı geri al
            if (comment.CommentStatus == "Onaylandı")
            {
                decimal removedCoin = (!string.IsNullOrWhiteSpace(comment.ImageUrl) || !string.IsNullOrWhiteSpace(comment.ImageUrl2)) ? 8 : 5;

                var userCoin = await _context.UserCoins.FirstOrDefaultAsync(x => x.UserId == comment.UserId);
                if (userCoin != null)
                {
                    userCoin.Coin -= removedCoin;
                    if (userCoin.Coin < 0) userCoin.Coin = 0;
                    userCoin.LastUpdated = DateTime.Now;
                    _context.UserCoins.Update(userCoin);
                }

                // Coin geri alma bildirimi
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == comment.ProductId);
                string productName = product?.Name ?? "Bilinmeyen Ürün";

                var coinNotification = new Notification
                {
                    UserId = comment.UserId,
                    Title = "Para Puan Geri Alındı",
                    Message = $"\"{productName}\" ürününe yaptığınız yorum silindiği için {removedCoin} para puanınız geri alındı.",
                    NotificationType = "Coin",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(coinNotification);
            }

            // Yorum silindi
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }



    }
}