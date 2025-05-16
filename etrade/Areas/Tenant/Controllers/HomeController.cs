using System;
using System.Threading.Tasks;
using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Route("Tenant/[controller]/[action]")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TenantContext _tenantContext;
        private readonly TenantService _tenantService;
        private readonly SeedDataService _seedDataService;


        public HomeController(
            ILogger<HomeController> logger,
            TenantContext tenantContext,
            TenantService tenantService,
            SeedDataService seedDataService
            )
        {
            _logger = logger;
            _tenantContext = tenantContext;
            _tenantService = tenantService;
            _seedDataService = seedDataService;

        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("{tenantId}")]
        public async Task<IActionResult> Index(int tenantId = 1)
        {
            try
            {
                await _tenantService.CreateTenantDatabaseAsync(tenantId);
                await _seedDataService.SeedAllAsync(tenantId);
                return Content($"Tenant ID {tenantId} için veritabanı başarıyla oluşturuldu.");

            }
            catch (Exception ex)
            {
                return Content($"Hata oluştu: {ex.Message}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");

        }
    }
}