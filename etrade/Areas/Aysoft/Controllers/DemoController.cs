using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace etrade.Areas.Aysoft.Controllers
{
    [Area("Aysoft")]
    [Authorize(AuthenticationSchemes = "CustomerCookie")]


    public class DemoController : Controller
    {
        private readonly TenantContext _context;
        private readonly UserManager<TenantUser> _userManager;
        private readonly RoleManager<TenantRole> _roleManager;

        public DemoController(
            TenantContext context,
            UserManager<TenantUser> userManager,
            RoleManager<TenantRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Demo/Request


        // Şehir listesi için AJAX endpoint
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Request()
        {
            var cities = _context.Iller.ToList();
            ViewBag.Cities = cities;
            return View();
        }

        // POST: Demo/Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Request(DemoRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Cities = _context.Iller.ToList(); // eksikse dropdown verisi tekrar yüklenir
                return View(model); // Hatalar varsa formu geri döndür
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kullanıcı oluştur (ASP.NET Identity)
                    var user = new TenantUser
                    {
                        UserName = model.Username,
                        Email = model.Email,
                        PhoneNumber = model.Phone,
                        EmailConfirmed = true
                    };

                    var createUserResult = await _userManager.CreateAsync(user, model.Password);

                    if (!createUserResult.Succeeded)
                    {
                        foreach (var error in createUserResult.Errors)
                        {
                            Console.WriteLine("Error creating user {0}: {1}", model.Username, error.Description);
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model); // Hatalar varsa formu geri döndür
                    }
                    // Customer rolünü kontrol et ve ata
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new TenantRole("Customer"));
                    }

                    await _userManager.AddToRoleAsync(user, "Customer");

                    var tenantCustomer = new TenantCustomer
                    {
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        CompanyName = model.CompanyName,
                        TaxNumber = model.TaxNumber,
                        TaxOffice = model.TaxOffice,
                        Address = model.Address,
                        IlId = model.IlId,
                        ZipCode = model.ZipCode,
                        UserId = user.Id // Burada kullanıcı ID'si doğru şekilde ilişkilendiriliyor
                    };

                    _context.TenantCustomers.Add(tenantCustomer);
                    await _context.SaveChangesAsync();


                    // DemoRequest oluştur
                    var demoRequest = new DemoRequest
                    {
                        TenantCustomerId = tenantCustomer.Id,
                        RequestDate = DateTime.Now,
                        RequestNote = model.RequestNote,
                        RequestStatus = "Beklemede",
                        DemoDays = model.DemoDays,
                        IsActive = true
                    };

                    _context.DemoRequests.Add(demoRequest);
                    await _context.SaveChangesAsync();


                    await transaction.CommitAsync();


                    TempData["SuccessMessage"] = "Demo talebiniz başarıyla alındı.";
                    return RedirectToAction("Request");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Hata mesajının içeriğini daha ayrıntılı logla
                    Console.WriteLine("Error occurred during the demo request process: {0}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", ex.InnerException.Message);
                    }
                    return StatusCode(500, new { success = false, message = "Bir hata oluştu: " + ex.Message });
                }
            }
        }
        public async Task<IActionResult> Result()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
                return RedirectToAction("Login", "Account");

            var tenantCustomerId = await _context.TenantCustomers
                .Where(tc => tc.UserId == identityUser.Id)
                .Select(tc => tc.Id)
                .FirstOrDefaultAsync();

            if (tenantCustomerId == 0)
                return View("Error", "Bir hata oluştu: Kullanıcıya ait müşteri kaydı bulunamadı.");

            var demoRequest = await _context.DemoRequests
                .Where(dr => dr.TenantCustomerId == tenantCustomerId)
                .OrderByDescending(dr => dr.RequestDate)
                .FirstOrDefaultAsync();

            if (demoRequest == null)
                ViewBag.Message = "Henüz bir demo talebiniz bulunmamaktadır.";

            // LicenseType bilgisini çekiyoruz
            var licenseType = await _context.Licenses
                .Where(l => l.CustomerId == tenantCustomerId)
                .OrderByDescending(l => l.StartDate) // varsa birden fazla kayıt için en günceli
                .Select(l => l.LicenseType)
                .FirstOrDefaultAsync();

            ViewBag.LicenseType = licenseType ?? "Lisans bilgisi bulunamadı";

            return View(demoRequest);
        }
    }
}