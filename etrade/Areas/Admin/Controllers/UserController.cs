using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class UserController : Controller
    {

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public UserController(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var editorUsers = await _userManager.GetUsersInRoleAsync("Editor");

            var model = new UserManagementViewModel
            {
                Admins = adminUsers.ToList(),
                Editors = editorUsers.ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserCreateModel model)
        {
            var userCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName, // FullName bilgisi burada alınıyo
                    UserCode=userCode
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View("Index", await GetCurrentUsers());
        }

        [HttpPost]
        public IActionResult ChangeRole([FromForm] string userId, [FromForm] string newRole)
        {
            // Kullanıcının var olup olmadığını kontrol et
            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // Önce mevcut rolünü kaldır
            var currentRoles = _userManager.GetRolesAsync(user).Result;
            _userManager.RemoveFromRolesAsync(user, currentRoles).Wait();

            // Yeni rolü ata
            var result = _userManager.AddToRoleAsync(user, newRole).Result;
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Rol güncellenirken hata oluştu." });
        }


        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("Index", await GetCurrentUsers());
        }

        private async Task<UserManagementViewModel> GetCurrentUsers()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var editorUsers = await _userManager.GetUsersInRoleAsync("Editor");

            return new UserManagementViewModel
            {
                Admins = adminUsers.ToList(),
                Editors = editorUsers.ToList()
            };
        }



    }
}