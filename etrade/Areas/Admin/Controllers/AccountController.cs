using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace etrade.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly EtradeContext _context;


        public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/");
            return View();
        }
        public async Task<IActionResult> GetStoreLogos()
        {
            var storeSetting = await _context.StoreSettings.FirstOrDefaultAsync(ss => ss.Id == 1);
            if (storeSetting == null)
            {
                return NotFound("Logo ayarları bulunamadı.");
            }

            return Ok(new
            {
                logoDarkPath = storeSetting.LogoPath,
            });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz e-posta veya şifre.";
                    return RedirectToAction("Login");
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (!(roles.Contains("admin") || roles.Contains("editor")))
                {
                    TempData["ErrorMessage"] = "Sadece Admin veya Editor rolüne sahip kullanıcılar Admin paneline giriş yapabilir.";
                    return RedirectToAction("Login");
                }

                if (!user.IsActive)
                {
                    user.IsActive = true;
                    await _userManager.UpdateAsync(user);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, string.Join(",", roles))
            };

                    var identity = new ClaimsIdentity(claims, "AdminCookie");
                    var principal = new ClaimsPrincipal(identity);
                    var properties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(30),
                        AllowRefresh = true
                    };

                    await HttpContext.SignInAsync("AdminCookie", principal, properties);

                    // Direkt olarak Admin paneline yönlendir
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                else
                {
                    TempData["ErrorMessage"] = "Geçersiz giriş denemesi.";
                }
            }

            return View(model);
        }
        // Logout işlemi
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookie"); // Sadece admin cookie'yi sil
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
        public async Task<IActionResult> ShowRoles()
        {
            var user = await _userManager.FindByEmailAsync("editor@editor.com");

            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine(string.Join(", ", roles));  // Konsola roller yazdırılır

            return Ok(roles);  // Geriye rol listesi dönebilirsiniz
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
