using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class CouponController : Controller
    {
        private readonly EtradeContext _context;

        public CouponController(IHttpContextAccessor httpContextAccessor)
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
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CouponCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        success = false,
                        message = "Geçersiz veri girişi",
                        errors = errors
                    });
                }

                // Kupon kodunun benzersiz olduğunu kontrol et
                if (await _context.Coupons.AnyAsync(c => c.Code == model.Code.ToUpper()))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bu kupon kodu zaten kullanılıyor"
                    });
                }

                // Tarih kontrolü
                if (model.EndDate <= model.StartDate)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bitiş tarihi başlangıç tarihinden sonra olmalıdır"
                    });
                }

                // Geçerli kategorileri tanımla
                var validCategories = new List<string> {
            "FirstPurchase",
            "LoyalCustomer",
            "BasketDiscount",
        };

                // Kategori kontrolü
                if (!validCategories.Contains(model.CouponCategory))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Geçersiz kupon kategorisi"
                    });
                }

                var coupon = new Coupon
                {
                    Code = model.Code.ToUpper(),
                    Description = model.Description,
                    DiscountValue = model.DiscountValue,
                    MinimumOrderAmount = model.MinimumOrderAmount,
                    StartDate = model.StartDate.Date,
                    EndDate = model.EndDate.Date,
                    IsActive = model.IsActive,
                    MaxUsageCount = model.MaxUsageCount,
                    CouponCategory = model.CouponCategory,
                    CurrentUsageCount = 0
                };
                Console.WriteLine($"Code: {coupon.Code}");
                Console.WriteLine($"Description: {coupon.Description}");
                Console.WriteLine($"DiscountValue: {coupon.DiscountValue}");
                Console.WriteLine($"MinimumOrderAmount: {coupon.MinimumOrderAmount}");
                Console.WriteLine($"StartDate: {coupon.StartDate}");
                Console.WriteLine($"EndDate: {coupon.EndDate}");
                Console.WriteLine($"IsActive: {coupon.IsActive}");
                Console.WriteLine($"MaxUsageCount: {coupon.MaxUsageCount}");
                Console.WriteLine($"CouponCategory: {coupon.CouponCategory}");

                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kupon başarıyla oluşturuldu",
                    couponId = coupon.CouponId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Bir hata oluştu: " + ex.Message
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> List()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpGet]
        public async Task<IActionResult> GetCoupons(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string search = "",
    [FromQuery] string status = "all",
    [FromQuery] string sortBy = "newest")
        {
            try
            {
                var query = _context.Coupons.AsQueryable();

                // Arama filtresi
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c =>
                        c.Code.Contains(search) ||
                        (c.Description != null && c.Description.Contains(search)) ||
                        c.CouponCategory.Contains(search));
                }

                // Durum filtresi
                if (status != "all")
                {
                    bool isActive = status == "active";
                    query = query.Where(c => c.IsActive == isActive);
                }

                // Sıralama
                query = sortBy switch
                {
                    "name" => query.OrderBy(c => c.Code),
                    "name_desc" => query.OrderByDescending(c => c.Code),
                    "expiry" => query.OrderBy(c => c.EndDate),
                    "expiry_desc" => query.OrderByDescending(c => c.EndDate),
                    _ => query.OrderByDescending(c => c.CouponId) // newest
                };

                var totalCount = await query.CountAsync();

                // First get the data without the DaysRemaining calculation
                var couponData = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.CouponId,
                        c.Code,
                        c.Description,
                        c.DiscountValue,
                        c.MinimumOrderAmount,
                        c.StartDate,
                        c.EndDate,
                        c.IsActive,
                        c.CouponCategory,
                        c.CurrentUsageCount,
                        c.MaxUsageCount
                    })
                    .ToListAsync();

                // Then project to DTO with proper DaysRemaining calculation
                var coupons = couponData.Select(c => new CouponListDto
                {
                    Id = c.CouponId,
                    Code = c.Code,
                    Description = c.Description,
                    DiscountValue = c.DiscountValue.ToString("N2") + "₺",
                    MinimumOrderAmount = c.MinimumOrderAmount.HasValue ?
                        c.MinimumOrderAmount.Value.ToString("N2") + "₺" : "-",
                    StartDate = c.StartDate.ToString("dd.MM.yyyy"),
                    EndDate = c.EndDate.ToString("dd.MM.yyyy"),
                    IsActive = c.IsActive,
                    CouponCategory = c.CouponCategory,
                    CurrentUsageCount = c.CurrentUsageCount,
                    MaxUsageCount = c.MaxUsageCount,
                    DaysRemaining = Math.Max((c.EndDate.Date - DateTime.Today.Date).Days, 0)
                }).ToList();

                return Ok(new PagedResponse<CouponListDto>
                {
                    Data = coupons,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Bir hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpDelete]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var coupon = await _context.Coupons.FindAsync(id);
                if (coupon == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Kupon bulunamadı"
                    });
                }

                // Kullanımda olan kupon kontrolü
                if (coupon.CurrentUsageCount > 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Kullanımda olan kupon silinemez"
                    });
                }

                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kupon başarıyla silindi"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Silme işlemi sırasında bir hata oluştu",
                    error = ex.Message
                });
            }
        }

        public class CouponListDto
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string DiscountValue { get; set; }
            public string MinimumOrderAmount { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public bool IsActive { get; set; }
            public string CouponCategory { get; set; }
            public int CurrentUsageCount { get; set; }
            public int MaxUsageCount { get; set; }
            public DateTime CreatedDate { get; set; }
            public int DaysRemaining { get; set; }
        }

        public class PagedResponse<T>
        {
            public List<T> Data { get; set; }
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(int id, [Bind("CouponId,Code,Description,DiscountValue,MinimumOrderAmount,StartDate,EndDate,MaxUsageCount,CouponCategory,IsActive")] Coupon coupon)
        {
            if (id != coupon.CouponId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Mevcut kuponu veritabanından al
                    var existingCoupon = await _context.Coupons.FindAsync(id);
                    if (existingCoupon == null)
                    {
                        return NotFound();
                    }

                    // Değişiklikleri uygula
                    existingCoupon.Code = coupon.Code;
                    existingCoupon.Description = coupon.Description;
                    existingCoupon.DiscountValue = coupon.DiscountValue;
                    existingCoupon.MinimumOrderAmount = coupon.MinimumOrderAmount;
                    existingCoupon.StartDate = coupon.StartDate;
                    existingCoupon.EndDate = coupon.EndDate;
                    existingCoupon.MaxUsageCount = coupon.MaxUsageCount;
                    existingCoupon.CouponCategory = coupon.CouponCategory;
                    existingCoupon.IsActive = coupon.IsActive;

                    // Değişiklikleri kaydet
                    _context.Update(existingCoupon);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(List));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.CouponId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (!ModelState.IsValid)
            {
                // Hataları logla
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // Hataları konsola veya log dosyasına yazabilirsiniz
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(coupon);
            }
            return View(coupon);
        }


        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }

    }
}