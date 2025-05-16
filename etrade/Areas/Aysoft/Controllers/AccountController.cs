using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace etrade.Areas.Aysoft.Controllers
{
    [Area("Aysoft")]
    public class AccountController : Controller
    {
        private readonly SignInManager<TenantUser> _signInManager;
        private readonly UserManager<TenantUser> _userManager;

        public AccountController(SignInManager<TenantUser> signInManager, UserManager<TenantUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(CustomerLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Geçersiz giriş.");
                return View(model);
            }

            if (!await _userManager.IsInRoleAsync(user, "Customer"))
            {
                ModelState.AddModelError("", "Bu panel için yetkiniz yok.");
                return View(model);
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError("", "Geçersiz şifre.");
                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("CustomerAuth", "true") // Custom claim to identify customer auth
    };

            var claimsIdentity = new ClaimsIdentity(claims, "CustomerCookie");

            await HttpContext.SignInAsync(
                "CustomerCookie",
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home", new { area = "Aysoft" });
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CustomerCookie");
            return RedirectToAction("Login", "Account", new { area = "Aysoft" });
        }
    }

    public class CustomerLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
