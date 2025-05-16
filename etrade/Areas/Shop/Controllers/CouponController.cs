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


    public class CouponController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly EtradeContext _context;


        public CouponController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
        public async Task<IActionResult> GetActiveCoupons()
        {
            var userId = _userManager.GetUserId(User);

            // Kullanıcının kaydettiği kupon ID'leri
            var savedCouponIds = await _context.CouponUsages
                .Where(cu => cu.UserId == userId)
                .Select(cu => cu.CouponId)
                .ToListAsync();

            // CoinConversion olan ve kullanıcıya ait kuponlar
            var userCoinConversionCouponIds = await _context.CouponUsages
                .Where(cu => cu.UserId == userId && cu.Coupon.CouponCategory == "CoinConversion" && cu.Coupon.IsActive)
                .Select(cu => cu.CouponId)
                .ToListAsync();

            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive &&
                    (
                        c.CouponCategory != "CoinConversion" || // Diğer tüm kuponlar
                        userCoinConversionCouponIds.Contains(c.CouponId) // Sadece kullanıcıya ait CoinConversion kuponları
                    ))
                .OrderByDescending(c => c.CouponId) // ← Kuponları ID'ye göre sırala
                .Select(c => new
                {
                    c.CouponId,
                    c.Code,
                    c.Description,
                    c.DiscountValue,
                    c.MinimumOrderAmount,
                    c.StartDate,
                    c.EndDate,
                    c.CouponCategory,
                    c.MinimumProductCount,
                    c.MaxUsageCount,
                    c.CurrentUsageCount,
                    IsSaved = savedCouponIds.Contains(c.CouponId)
                })
                .ToListAsync();

            return Ok(activeCoupons);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Save(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Kupon ve ilişkili verileri tek sorguda çekiyoruz (performans için)
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.CouponId == id);

                if (coupon == null)
                {
                    return NotFound("Kupon bulunamadı.");
                }

                // Maksimum kullanım limiti kontrolü
                if (coupon.MaxUsageCount > 0 && coupon.CurrentUsageCount >= coupon.MaxUsageCount)
                {
                    return BadRequest("Bu kupon için maksimum kullanım sayısına ulaşıldı.");
                }

                // Kullanıcının daha önce bu kuponu kaydedip kaydetmediğini kontrol et
                var existingUsage = await _context.CouponUsages
                    .AnyAsync(cu => cu.CouponId == id && cu.UserId == userId);

                if (existingUsage)
                {
                    return BadRequest("Bu kupon zaten kaydedilmiş.");
                }

                // Transaction başlatıyoruz (atomic işlem için)
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Yeni kullanım kaydı oluştur
                    var couponUsage = new CouponUsage
                    {
                        CouponId = id,
                        UserId = userId,
                        UsageDate = DateTime.Now,
                        IsUsed = false
                    };

                    _context.CouponUsages.Add(couponUsage);

                    // Kuponun kullanım sayacını güncelle
                    coupon.CurrentUsageCount += 1;
                    _context.Coupons.Update(coupon);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw; // Dıştaki catch bloğuna yönlendir
                }
            }
            catch (Exception ex)
            {
                // Loglama yapılabilir
                return StatusCode(500, "Bir hata oluştu: " + ex.Message);
            }
        }
        public async Task<IActionResult> GetDetails(int id)
        {
            var coupon = await _context.Coupons
                .Where(c => c.CouponId == id)
                .FirstOrDefaultAsync();

            if (coupon == null)
            {
                // Loglama yapabilirsiniz
                Console.WriteLine($"Kupon ID: {id} bulunamadı.");
                return NotFound(new { message = "Kupon bulunamadı" });
            }

            return Ok(new
            {
                coupon.Code,
                coupon.DiscountValue,
                coupon.Description,
                coupon.MinimumOrderAmount,
                coupon.MinimumProductCount,
                coupon.MaxUsageCount,
                StartDate = coupon.StartDate.ToString("yyyy-MM-dd"),
                EndDate = coupon.EndDate.ToString("yyyy-MM-dd")
            });
        }
    }
}