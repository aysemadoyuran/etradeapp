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
using Newtonsoft.Json;

namespace etrade.Areas.Shop
{
    [Area("Shop")]
    [Authorize(Roles = "customer")]
    [Authorize(AuthenticationSchemes = "ShopCookie")]



    public class ProfileController : Controller
    {
        private readonly EtradeContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ProfileController> _logger;
        private readonly SignInManager<AppUser> _signInManager;


        public ProfileController(IHttpContextAccessor httpContextAccessor, ILogger<ProfileController> logger, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
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
            }            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;



        }
        public IActionResult MyAddress()
        {
            return View();
        }
        [HttpGet]
        public JsonResult GetIller()
        {
            var iller = _context.Iller.Select(i => new { i.Id, i.Ad }).ToList();
            return Json(iller);
        }

        [HttpGet]
        public JsonResult GetIlceler(int ilId)
        {
            var ilceler = _context.Ilceler.Where(i => i.IlId == ilId)
                                          .Select(i => new { i.Id, i.Ad }).ToList();
            return Json(ilceler);
        }

        [HttpGet]
        public JsonResult GetSemtler(int ilceId)
        {
            var semtler = _context.Districts
                                  .Where(s => s.IlceId == ilceId)
                                  .Include(s => s.Ilce) // Eager loading, ilişkiyi dahil et
                                  .ToList();

            // Debug için veriyi konsola yazdırın
            return Json(semtler.Select(s => new { s.Id, s.SemtAdi }).ToList());
        }

        [HttpGet]
        public JsonResult GetMahalleler(int semtId)
        {
            var mahalleler = _context.Streets.Where(m => m.SemtId == semtId)
                                                .Select(m => new { m.Id, m.MahalleAdi }).ToList();
            return Json(mahalleler);
        }
        [HttpPost]
        public IActionResult SaveAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz veri!" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Kullanıcı ID'sini alın
                _logger.LogInformation($"Kullanıcı ID'si: {userId}");

                var address = new Address
                {
                    UserId = userId,
                    NameSurname = model.NameSurname,
                    Title = model.AddressTitle,
                    IlId = model.CityId,
                    IlceId = model.DistrictId,
                    SemtId = model.NeighborhoodId,
                    MahalleId = model.VillageId,
                    Telefon = model.PhoneNumber,
                    AcikAdres = model.AddressDetail,
                };

                _context.Add(address);
                _context.SaveChanges();

                return RedirectToAction("MyAddress"); // Başka bir sayfaya yönlendirebilirsiniz
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"İlçe ID'si: {model.DistrictId}"); // İlçe ID'sini logla
                _logger.LogError($"Adres kaydedilirken hata oluştu: {ex.Message}, Inner Exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { message = "Adres kaydedilirken sunucu hatası oluştu." });
            }
        }
        [HttpGet]
        public IActionResult GetUserAddresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addresses = _context.Address
                                    .Where(a => a.UserId == userId)
                                    .Include(a => a.Il)  // İl bilgisi
                                    .Include(a => a.Ilce)  // İlçe bilgisi
                                    .Include(a => a.District)  // Semt bilgisi
                                    .Include(a => a.Street)  // Mahalle bilgisi
                                    .ToList();

            var addressViewModels = addresses.Select(a => new
            {
                a.Id,
                a.Title,

                City = a.Il.Ad,
                CityId = a.Il.Id,

                DistrictId = a.Ilce.Id,
                District = a.Ilce.Ad,

                NeighborhoodId = a.District.Id,
                Neighborhood = a.District.SemtAdi,

                VillageId = a.Street.Id,
                Village = a.Street.MahalleAdi,

                a.Telefon,
                a.AcikAdres,
                a.NameSurname
            }).ToList();

            return Json(addressViewModels);
        }
        [HttpPost]
        public IActionResult DeleteAddress(int id)
        {
            var address = _context.Address.FirstOrDefault(a => a.Id == id);
            if (address != null)
            {
                _context.Address.Remove(address);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Adres bulunamadı" });
        }


        [HttpPost]
        public IActionResult UpdateAddress(AddressViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Adresin ID'sini alalım
                var address = _context.Address.Find(model.Id);
                if (address != null)
                {
                    // Adres bilgilerini güncelleme
                    address.NameSurname = model.NameSurname;
                    address.Title = model.AddressTitle;
                    address.IlId = model.CityId;
                    address.IlceId = model.DistrictId;
                    address.SemtId = model.NeighborhoodId;
                    address.MahalleId = model.VillageId;
                    address.Telefon = model.PhoneNumber;
                    address.AcikAdres = model.AddressDetail;

                    _context.SaveChanges();

                    return RedirectToAction("MyAddress"); // Başka bir sayfaya yönlendirebilirsiniz
                }
                return NotFound(new { success = false, message = "Adres bulunamadı." });
            }
            return BadRequest(new { success = false, message = "Geçersiz veri." });
        }
        public IActionResult MyAccount()
        {
            var user = _userManager.GetUserAsync(User).Result;
            var model = new UserInfoViewModel
            {
                Username = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };
            return View(model);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MyAccount(UserInfoViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;

                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        TempData["SuccessMessage"] = "Bilgileriniz başarıyla güncellendi.";
                        return RedirectToAction("MyAccount");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                    }
                }
            }
            return View(model);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeactivateAccount()
        {
            // Kullanıcı bilgilerini al
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Kullanıcı bulunamazsa hata mesajı döndür
                return Unauthorized();
            }

            // Hesabı dondur
            user.IsActive = false;

            // Kullanıcıyı güncelle
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                // Güncelleme başarısızsa hata mesajı döndür
                return BadRequest("Hesabınızı dondururken bir sorun oluştu.");
            }

            // Kullanıcıyı çıkış yaptır
            await _signInManager.SignOutAsync();

            // Başarı mesajı
            TempData["ErrorMessage"] = "Hesabınız başarıyla donduruldu. Tekrar aktif etmek için giriş yapabilirsiniz.";

            // Giriş sayfasına yönlendir
            return RedirectToAction("Login", "Account");
        }
        [HttpGet]
        [Authorize]
        public IActionResult Password()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Eğer model geçersizse, kullanıcıyı aynı sayfaya yönlendir.
            }

            var user = await _userManager.GetUserAsync(User); // Giriş yapmış kullanıcıyı al

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Şifre başarılı şekilde değiştirildi
                await _signInManager.RefreshSignInAsync(user); // Kullanıcıyı tekrar giriş yapmış olarak işaretle
                TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirilmiştir.";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Hata durumunda, hataları modelstate'e ekle
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

        }
    }
}