using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace etrade.Areas.Aysoft.Controllers
{
    [Area("Aysoft")]
    [Authorize(AuthenticationSchemes = "CustomerCookie")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly TenantContext _context;



        public HomeController(ILogger<HomeController> logger, TenantContext context)
        {
            _logger = logger;
            _context = context;


        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Demo()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }

}