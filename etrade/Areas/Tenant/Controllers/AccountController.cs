using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using etrade.Entity;
using Microsoft.AspNetCore.Authorization;
using etrade.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<TenantUser> _userManager;
        private readonly SignInManager<TenantUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<TenantUser> userManager,
            SignInManager<TenantUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Username,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Tenant kullanıcı giriş yaptı: {Username}", model.Username);
                    
                    // Açıkça TenantCookie kullanarak giriş yap
                    await HttpContext.SignInAsync(
                        "TenantCookie",
                        new ClaimsPrincipal(await _signInManager.CreateUserPrincipalAsync(
                            await _userManager.FindByNameAsync(model.Username))),
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        });
                    
                    return RedirectToLocal(returnUrl);
                }
                
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            await HttpContext.SignOutAsync("TenantCookie");
            _logger.LogInformation("Tenant kullanıcı çıkış yaptı.");
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home", new { area = "Tenant" });
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}