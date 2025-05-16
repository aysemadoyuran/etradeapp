using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace etrade.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HataController : Controller
    {
        public IActionResult LisansYok()
        {
            return View();
        }
        public IActionResult ErisimKisitlamasi()
        {
            return View();
        }
        public IActionResult Odeme()
        {
            return View();
        }
        public IActionResult LisansSonlandirildi()
        {
            return View();
        }
        public IActionResult LisansDonduruldu()
        {
            return View();
        }
        public IActionResult LisansGunuDoldu()
        {
            return View();
        }

    }
}