using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]


    public class CoinController : Controller
    {
        private readonly EtradeContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CoinController(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
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
            _userManager = userManager;
        }
        public async Task<IActionResult> GetUserCoins()
        {
            // Kullanıcıyı Claim üzerinden alıyoruz
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier); // UserId genellikle NameIdentifier claim'inde tutulur
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); // Kullanıcı ID'si bulunamazsa Unauthorized döndür
            }

            // Kullanıcıyı veritabanından alalım
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // UserCoins tablosunda ilgili coin kaydını arıyoruz
            var userCoin = await _context.UserCoins
                .Include(uc => uc.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCoin == null)
            {
                // Coin kaydı yoksa, 0 coin ve userCode döndürüyoruz
                return Ok(new { currentCoin = 0, userCode = user.UserCode });
            }

            return Ok(new
            {
                currentCoin = userCoin.Coin,
                userCode = userCoin.User?.UserCode
            });
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ConvertToVoucher([FromBody] ConvertToVoucherRequest request)
        {
            // 1. Kullanıcı bilgilerini al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users
                .Include(u => u.UserCoin)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Kullanıcı bulunamadı" });
            }

            // 2. Coin kontrolü yap (kullanıcının yeterli coin'i var mı?)
            if (user.UserCoin.Coin < request.Price)
            {
                return BadRequest(new { success = false, message = "Yetersiz coin bakiyesi" });
            }

            // 3. Benzersiz kupon kodu oluştur
            var couponCode = GenerateCouponCode(8);

            // 4. Yeni kupon oluştur
            var coupon = new Coupon
            {
                Code = couponCode,
                Description = $"Coin ile alınan {request.Amount}₺ değerinde hediye çeki",
                DiscountValue = request.Amount,
                MinimumOrderAmount = request.MinOrder,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(15), // 15 gün geçerli
                IsActive = true,
                MaxUsageCount = 1, // Sadece 1 kez kullanılabilir
                CurrentUsageCount = 0,
                CouponCategory = "CoinConversion"
            };

            // 5. Kupon kullanım kaydı oluştur
            var couponUsage = new CouponUsage
            {
                Coupon = coupon,
                UserId = userId,
                UsageDate = DateTime.Now,
                IsUsed = false
            };

            // 6. Kullanıcının coin bakiyesini güncelle
            user.UserCoin.Coin -= request.Price;

            // 7. Tüm değişiklikleri veritabanına kaydet
            try
            {
                _context.Coupons.Add(coupon);
                _context.CouponUsages.Add(couponUsage);
                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    couponCode = coupon.Code,
                    amount = coupon.DiscountValue,
                    expiryDate = coupon.EndDate.ToString("dd.MM.yyyy")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Kupon oluşturulurken hata: " + ex.Message });
            }
        }

        // Benzersiz kupon kodu oluşturma metodu
        private string GenerateCouponCode(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // O ve 1 gibi karışabilecek karakterler çıkarıldı
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        // Request modeli
        public class ConvertToVoucherRequest
        {
            public int Amount { get; set; } // Hediye çeki miktarı (200₺, 300₺ gibi)
            public int Price { get; set; }  // Kaç coin gerektiği (200 coin, 300 coin gibi)
            public int MinOrder { get; set; } // Minimum sipariş tutarı
        }
    }
}