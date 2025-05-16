using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class CampaignReportController : Controller
    {
        private readonly EtradeContext _context;

        public CampaignReportController(IHttpContextAccessor httpContextAccessor)
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
            }
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetCampaignReport()
        {
            int discountId = 48;
            var campaign = await _context.Discounts
                .Where(d => d.Id == discountId) // discountId'yi dinamik alıyoruz
                .FirstOrDefaultAsync();

            if (campaign == null)
            {
                return NotFound("Kampanya bulunamadı veya aktif değil.");
            }

            // Kampanya tarihlerini UTC'ye dönüştürme
            var campaignStartDateUtc = campaign.StartDateTime.ToUniversalTime();
            var campaignEndDateUtc = campaign.EndDateTime.ToUniversalTime();

            // Kampanya süresince satış verilerini al (Zaman dilimi esnekliği ekleyerek)
            var campaignSalesDuringPeriod = await _context.OrderItems
                .Where(oi => oi.DiscountId == discountId
                             && oi.Order.OrderDate >= campaignStartDateUtc
                             && oi.Order.OrderDate <= campaignEndDateUtc.AddMinutes(5)) // Bitiş tarihi sonrası 5 dakika ekleme
                .GroupBy(oi => oi.ProductVariantId)
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                }).ToListAsync();

            // Kampanya öncesindeki satış ortalamasını al
            var averageSalesBeforeCampaign = await _context.OrderItems
                .Where(oi => oi.DiscountId == discountId && oi.Order.OrderDate < campaignStartDateUtc)
                .GroupBy(oi => oi.ProductVariantId)
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    AverageQuantitySold = g.Average(oi => oi.Quantity),
                    AverageRevenue = g.Average(oi => oi.TotalPrice)
                }).ToListAsync();

            // Kampanya skoru hesaplaması
            var totalSalesDuringCampaign = campaignSalesDuringPeriod.Sum(ps => ps.TotalRevenue);
            var totalRevenueBeforeCampaign = averageSalesBeforeCampaign.Sum(oi => oi.AverageRevenue);
            var campaignScore = totalRevenueBeforeCampaign > 0
                ? (totalSalesDuringCampaign - totalRevenueBeforeCampaign) * 100 / totalRevenueBeforeCampaign
                : 0;

            // En çok satılan ürünü bul
            var mostSoldProduct = await _context.OrderItems
                .Where(oi => oi.DiscountId == discountId
                             && oi.Order.OrderDate >= campaignStartDateUtc
                             && oi.Order.OrderDate <= campaignEndDateUtc.AddMinutes(5)) // Bitiş tarihi sonrası 5 dakika ekleme
                .GroupBy(oi => oi.ProductVariantId)
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.TotalQuantitySold) // En çok satılan ürünü alıyoruz
                .FirstOrDefaultAsync();

            // Ürün adını almak için ProductVariantId'yi kullanıyoruz
            var mostSoldProductDetails = await _context.ProductVariants
                .Where(pv => pv.Id == mostSoldProduct.ProductVariantId)  // En çok satılan ürünün ProductVariantId'si
                .Select(pv => new
                {
                    pv.Id,
                    ProductName = pv.Product.Name, // Ürün adını alıyoruz
                    mostSoldProduct.TotalQuantitySold,
                    mostSoldProduct.TotalRevenue
                })
                .FirstOrDefaultAsync();


            // Kampanya süresince ve öncesinde günlük ortalama ciroyu hesapla
            var daysInCampaign = (campaignEndDateUtc - campaignStartDateUtc).Days;
            var averageDailyRevenueDuringCampaign = daysInCampaign > 0
                ? totalSalesDuringCampaign / daysInCampaign
                : 0;

            // Kampanya öncesindeki 30 günün toplam ciroyu al
            var previousPeriodStartDate = campaignStartDateUtc.AddDays(-30); // Önceki 30 gün
            var totalRevenueBeforeCampaignPeriod = await _context.OrderItems
                .Where(oi => oi.DiscountId == discountId && oi.Order.OrderDate >= previousPeriodStartDate && oi.Order.OrderDate < campaignStartDateUtc)
                .SumAsync(oi => oi.TotalPrice);

            var daysBeforeCampaign = (campaignStartDateUtc - previousPeriodStartDate).Days;
            var averageDailyRevenueBeforeCampaign = daysBeforeCampaign > 0
                ? totalRevenueBeforeCampaignPeriod / daysBeforeCampaign
                : 0;

            // Ürün bazında satış karşılaştırması
            var productSalesComparison = campaignSalesDuringPeriod
                .GroupJoin(averageSalesBeforeCampaign,
                    ps => ps.ProductVariantId,
                    avg => avg.ProductVariantId,
                    (ps, avg) => new
                    {
                        ProductVariantId = ps.ProductVariantId,
                        SalesDuringCampaign = ps.TotalQuantitySold,
                        AverageSalesBeforeCampaign = avg.DefaultIfEmpty().FirstOrDefault()?.AverageQuantitySold ?? 0, // Eğer önceki satış verisi yoksa 0
                        SalesDifference = ps.TotalQuantitySold - (avg.DefaultIfEmpty().FirstOrDefault()?.AverageQuantitySold ?? 0), // Önceki satış verisi yoksa 0
                        ProductName = _context.ProductVariants
                            .Where(pv => pv.Id == ps.ProductVariantId)
                            .Select(pv => pv.Product.Name)
                            .FirstOrDefault() // Ürün ismini al
                    })
                .ToList(); // Veriyi liste haline getir

            // Raporu JSON formatında döndür
            var report = new
            {
                CampaignName = campaign.Name,
                CampaignStartDate = campaign.StartDateTime,
                CampaignEndDate = campaign.EndDateTime,
                TotalSalesDuringCampaign = totalSalesDuringCampaign,
                TotalRevenueBeforeCampaign = totalRevenueBeforeCampaign,
                CampaignScore = campaignScore,
                MostSoldProductDetails = mostSoldProductDetails,
                AverageDailyRevenueDuringCampaign = averageDailyRevenueDuringCampaign,
                AverageDailyRevenueBeforeCampaign = averageDailyRevenueBeforeCampaign,
                ProductSalesComparison = productSalesComparison
            };

            return Json(report); // JSON formatında döndürülüyor
        }
    }
}