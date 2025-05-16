using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using etrade.Data.Concrete;
using etrade.Migrations;
using Microsoft.AspNetCore.Authentication;
using etrade.Entity;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]

    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EtradeContext _context;
        private readonly TenantContext _tenantContext;


        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, TenantContext tenantContext)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _tenantContext = tenantContext;
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
        public async Task<string> GenerateUniqueUsername()
        {
            string username;
            bool exists;
            do
            {
                username = "user" + Guid.NewGuid().ToString("N").Substring(0, 5);
                exists = await _userManager.Users.AnyAsync(u => u.UserName == username);
            } while (exists); // Aynı username varsa tekrar üret

            return username;
        }

        // Login View
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl = Url.Action("Index", "Home", new { area = "Shop" });
            if (ModelState.IsValid)
            {
                // Önceki oturumu tamamen temizle
                await HttpContext.SignOutAsync("ShopCookie");
                HttpContext.Session.Clear();

                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                    return View(model);
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("customer"))
                {
                    ModelState.AddModelError(string.Empty, "Sadece müşteri kullanıcıları Shop alanına giriş yapabilir.");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    user.IsActive = true;
                    await _userManager.UpdateAsync(user);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Sepet bilgilerini temizle (eğer gerekiyorsa)
                    HttpContext.Session.Remove("CartItems");

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "customer")
            };

                    var identity = new ClaimsIdentity(claims, "ShopCookie");
                    var principal = new ClaimsPrincipal(identity);

                    var properties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
                        AllowRefresh = true
                    };

                    await HttpContext.SignInAsync("ShopCookie", principal, properties);

                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz şifre.");
                }
            }

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Tüm authentication cookie'lerini temizle
            await HttpContext.SignOutAsync("ShopCookie");

            // SignInManager ile de çıkış yap
            await _signInManager.SignOutAsync();

            // Session'ı temizle
            HttpContext.Session.Clear();

            // Tarayıcı cache'ini temizlemek için header'ları ayarla
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "Account", new { area = "Shop" });
        }

        // Register View
        public async Task<IActionResult> Register()
        {
            var domain = HttpContext.Request.Host.Host; // Örn: demo.magaza.com
            var tenantStore = await _tenantContext.TenantStores
                .FirstOrDefaultAsync(t => t.Domain == domain);

            if (tenantStore == null)
            {
                return NotFound("Mağaza bulunamadı.");
            }

            var license = await _tenantContext.Licenses
                .FirstOrDefaultAsync(l => l.Id == tenantStore.LicenseId);

            if (license == null)
            {
                return NotFound("Lisans bilgisi bulunamadı.");
            }

            if (license.LicenseType == "Demo") // Veya Enum ise license.Type == LicenseType.Demo
            {
                return RedirectToAction("Index", "Home"); // veya özel bir sayfa
            }

            return View();
        }

        // Term of Use View
        public async Task<IActionResult> TermOfUse()
        {
            return View();
        }

        // Register POST Method
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var domain = HttpContext.Request.Host.Host;
            var tenantStore = await _tenantContext.TenantStores.FirstOrDefaultAsync(t => t.Domain == domain);

            if (tenantStore == null)
            {
                ModelState.AddModelError(string.Empty, "Mağaza bilgisi bulunamadı.");
                return View(model);
            }

            var license = await _tenantContext.Licenses.FirstOrDefaultAsync(l => l.Id == tenantStore.LicenseId);

            if (license == null)
            {
                ModelState.AddModelError(string.Empty, "Lisans bilgisi bulunamadı.");
                return View(model);
            }

            if (license.LicenseType == "Demo") // Enum ise: license.Type == LicenseType.Demo
            {
                return RedirectToAction("Index", "Home"); // veya özel bir uyarı sayfası gösterebilirsin
            }
            //
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine("Hata: " + error.ErrorMessage);
                }
                return View(model);
            }

            // E-posta kontrolü
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresiyle zaten bir hesap oluşturulmuş.");
                return View(model);
            }

            // Benzersiz 8 haneli davet kodu oluştur
            string userCode = await GenerateUniqueUserCode();

            var user = new AppUser
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = await GenerateUniqueUsername(),
                UserCode = userCode,
                InvitationCode = model.InvitationCode // kullanıcı davet kodu girdiyse
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Kullanıcıyı kaydet
                await _context.SaveChangesAsync(); // Kullanıcı kaydını kaydediyoruz

                // Yeni kayıt olan kullanıcıya 10 coin veriyoruz
                var userCoin = new UserCoin
                {
                    UserId = user.Id,
                    Coin = 10,
                    LastUpdated = DateTime.Now
                };
                _context.UserCoins.Add(userCoin);

                // Sepet oluşturuluyor
                var basket = new Entity.Basket
                {
                    UserId = user.Id, // Kullanıcı ID'sini buraya ekliyoruz
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Baskets.Add(basket);

                await _context.SaveChangesAsync(); // Sepeti kaydediyoruz

                // Eğer geçerli bir davet kodu girilmişse, o davet kodunun sahibine 10 coin veriyoruz (ilk 5 kullanım için)
                if (!string.IsNullOrEmpty(model.InvitationCode))
                {
                    var inviterUser = await _userManager.Users
                        .Include(u => u.UserCoin)
                        .FirstOrDefaultAsync(u => u.UserCode == model.InvitationCode);

                    if (inviterUser != null)
                    {
                        var usageCount = await _context.Users
                            .CountAsync(u => u.InvitationCode == model.InvitationCode);

                        if (usageCount <= 5)
                        {
                            if (inviterUser.UserCoin == null)
                            {
                                inviterUser.UserCoin = new UserCoin
                                {
                                    UserId = inviterUser.Id,
                                    Coin = 10,
                                    LastUpdated = DateTime.Now
                                };
                                _context.UserCoins.Add(inviterUser.UserCoin);
                            }
                            else
                            {
                                inviterUser.UserCoin.Coin += 10;
                                inviterUser.UserCoin.LastUpdated = DateTime.Now;
                                _context.UserCoins.Update(inviterUser.UserCoin);

                                // Kullanıcıya 10 coin kazandığına dair bildirim gönder
                                var invitationNotification = new Entity.Notification
                                {
                                    UserId = inviterUser.Id,
                                    Title = "Davet Kodunuzla 10 Para Puan Kazandınız!",
                                    Message = $"Bir kullanıcı sizin davet kodunuzu kullanarak kayıt oldu. 10 para puan kazandınız! Kalan hakkınız: {5 - usageCount}",
                                    CreatedAt = DateTime.UtcNow,
                                    IsRead = false,
                                    IsGlobal = false,
                                    NotificationType = "Coin"
                                };

                                _context.Notifications.Add(invitationNotification);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync(); // Son değişiklikleri kaydediyoruz

                await _userManager.AddToRoleAsync(user, "customer");
                await _signInManager.SignInAsync(user, isPersistent: false);

                // SHOP COOKIE EKLEME
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "customer")
        };

                var identity = new ClaimsIdentity(claims, "ShopCookie");
                var principal = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync("ShopCookie", principal, properties);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Benzersiz 8 haneli UserCode oluşturma
        private async Task<string> GenerateUniqueUserCode()
        {
            string code;
            var random = new Random();

            do
            {
                code = Guid.NewGuid().ToString("N").Substring(0, 8);
            }
            while (await _userManager.Users.AnyAsync(u => u.UserCode == code));

            return code;
        }
    }
}