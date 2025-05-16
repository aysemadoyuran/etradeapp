using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class CommentController : Controller
    {
        private readonly EtradeContext _context;

        public CommentController(IHttpContextAccessor httpContextAccessor)
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
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult GetUserRole()
        {
            var isAdmin = User.IsInRole("admin"); // Kullanıcı Admin mi?
            return Json(new { isAdmin });
        }
        [HttpGet]
        public async Task<IActionResult> GetComments(int page = 1, int pageSize = 10, string filterStatus = "All")
        {
            // Başlangıçta sorguyu alıyoruz
            var commentsQuery = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Product)
                .Select(c => new
                {
                    c.Id,
                    c.UserId,
                    UserName = c.User.UserName,
                    c.ProductId,
                    ProductName = c.Product.Name,
                    c.Text,
                    c.ImageUrl,
                    c.ImageUrl2,
                    c.CreatedAt,
                    c.Rating,
                    c.CommentStatus
                });
            commentsQuery = commentsQuery.OrderByDescending(c => c.Id);
            // Filtreleme işlemi (Beklemede, Onaylanan, Reddedilen)
            if (filterStatus != "All")
            {
                commentsQuery = commentsQuery.Where(c => c.CommentStatus == filterStatus);
            }

            // Toplam yorum sayısını alıyoruz (sayfalama için)
            var totalComments = await commentsQuery.CountAsync();

            var comments = await commentsQuery
                .Skip((page - 1) * pageSize) // Sayfa başına kaç yorum gösterileceğini belirliyoruz
                .Take(pageSize) // İstenilen sayıda yorum alıyoruz
                .ToListAsync();

            // Sayfa sayısını hesaplıyoruz
            var totalPages = (int)Math.Ceiling((double)totalComments / pageSize);

            // Yorumlar ve toplam sayfa bilgisi ile birlikte sonucu döndürüyoruz
            return Ok(new
            {
                comments,
                totalPages,
                currentPage = page
            });
        }
        public class UpdateCommentStatusRequest
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }

        [HttpPost]
        public IActionResult UpdateCommentStatus([FromBody] UpdateCommentStatusRequest request)
        {
            if (request == null || request.Id == 0 || string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { message = "Geçersiz veri." });
            }

            var comment = _context.Comments.FirstOrDefault(c => c.Id == request.Id);
            if (comment == null)
            {
                return NotFound(new { message = "Yorum bulunamadı." });
            }

            string oldStatus = comment.CommentStatus;
            string newStatus = request.Status;

            if (oldStatus == newStatus)
            {
                return Ok(new { message = "Yorum durumu zaten bu şekilde." });
            }

            comment.CommentStatus = newStatus;
            _context.Comments.Update(comment);

            // Önce yorumu güncelle ve kaydet
            _context.SaveChanges();

            var product = _context.Products.FirstOrDefault(p => p.Id == comment.ProductId);
            string productName = product?.Name ?? "Bilinmeyen Ürün";

            // Coin işlemleri
            if (oldStatus == "Beklemede" && newStatus == "Onaylandı")
            {
                decimal earnedCoin = (!string.IsNullOrWhiteSpace(comment.ImageUrl) || !string.IsNullOrWhiteSpace(comment.ImageUrl2)) ? 8 : 5;

                var userCoin = _context.UserCoins.FirstOrDefault(x => x.UserId == comment.UserId);
                if (userCoin != null)
                {
                    userCoin.Coin += earnedCoin;
                    userCoin.LastUpdated = DateTime.Now;
                    _context.UserCoins.Update(userCoin);
                }
                else
                {
                    _context.UserCoins.Add(new UserCoin
                    {
                        UserId = comment.UserId,
                        Coin = earnedCoin,
                        LastUpdated = DateTime.Now
                    });
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = comment.UserId,
                    Title = "Yorum Güncellemesi",
                    Message = $"\"{productName}\" ürününe yaptığınız yorum onaylandı ve {earnedCoin} para puan kazandınız.",
                    NotificationType = "Coin",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            else if (oldStatus == "Onaylandı" && newStatus == "Reddedildi")
            {
                decimal removedCoin = (!string.IsNullOrWhiteSpace(comment.ImageUrl) || !string.IsNullOrWhiteSpace(comment.ImageUrl2)) ? 8 : 5;

                var userCoin = _context.UserCoins.FirstOrDefault(x => x.UserId == comment.UserId);
                if (userCoin != null)
                {
                    userCoin.Coin -= removedCoin;
                    if (userCoin.Coin < 0) userCoin.Coin = 0;
                    userCoin.LastUpdated = DateTime.Now;
                    _context.UserCoins.Update(userCoin);
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = comment.UserId,
                    Title = "Para Puan Geri Alındı",
                    Message = $"\"{productName}\" ürününe yaptığınız yorum reddedildiği için {removedCoin} para puanınız geri alındı.",
                    NotificationType = "Coin",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            else if (oldStatus == "Reddedildi" && newStatus == "Onaylandı")
            {
                decimal earnedCoin = (!string.IsNullOrWhiteSpace(comment.ImageUrl) || !string.IsNullOrWhiteSpace(comment.ImageUrl2)) ? 8 : 5;

                var userCoin = _context.UserCoins.FirstOrDefault(x => x.UserId == comment.UserId);
                if (userCoin != null)
                {
                    userCoin.Coin += earnedCoin;
                    userCoin.LastUpdated = DateTime.Now;
                    _context.UserCoins.Update(userCoin);
                }
                else
                {
                    _context.UserCoins.Add(new UserCoin
                    {
                        UserId = comment.UserId,
                        Coin = earnedCoin,
                        LastUpdated = DateTime.Now
                    });
                }

                _context.Notifications.Add(new Notification
                {
                    UserId = comment.UserId,
                    Title = "Para Puan Kazandınız!",
                    Message = $"\"{productName}\" ürününe yaptığınız yorum onaylandı ve {earnedCoin} para puan kazandınız.",
                    NotificationType = "Coin",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            // Şimdi ürünün yorum sayısı ve ortalama puanı yeniden hesapla
            if (product != null)
            {
                var onayliYorumlar = _context.Comments
                    .Where(c => c.ProductId == product.Id && c.CommentStatus == "Onaylandı")
                    .ToList();

                product.CommentCount = onayliYorumlar.Count;

                var ratedComments = onayliYorumlar.Where(c => c.Rating > 0).ToList();
                product.AverageRating = ratedComments.Any() ? (decimal)ratedComments.Average(c => c.Rating) : 0;

                _context.Products.Update(product);
            }

            try
            {
                _context.SaveChanges();
                return Ok(new { message = "Yorum durumu güncellendi ve gerekli işlemler yapıldı." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "İşlem sırasında hata oluştu.",
                    error = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
        public IActionResult GetCommentById(int id)
        {
            var comment = _context.Comments.FirstOrDefault(c => c.Id == id);
            if (comment == null)
                return NotFound();
            return Ok(comment);
        }
    }
}