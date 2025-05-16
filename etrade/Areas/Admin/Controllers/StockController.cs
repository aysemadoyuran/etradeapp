using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin,editor")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]


    public class StockController : Controller
    {
        private readonly EtradeContext _context;

        public StockController(IHttpContextAccessor httpContextAccessor)
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
        public IActionResult StockList()
        {
            // Tüm ürün varyantlarını ve ilişkili stok bilgilerini alalım
            var productVariants = _context.ProductVariants
                                          .Include(pv => pv.Product)
                                          .Include(pv => pv.Color)
                                          .Include(pv => pv.Size)
                                          .ToList();

            return View(productVariants);
        }

        public IActionResult GetProductCategoryReport()
        {
            var categoryReport = _context.Products
                                        .GroupBy(p => p.CategoryId)  // Kategorilere göre gruplama yapıyoruz
                                        .Select(g => new
                                        {
                                            CategoryName = g.FirstOrDefault().Category.Name,  // İlk üründen kategori adını alıyoruz
                                            ProductCount = g.Count()  // Ürün sayısını alıyoruz
                                        })
                                        .ToList();

            var subCategoryReport = _context.Products
                                             .GroupBy(p => p.SubCategoryId)  // Alt kategoriye göre gruplama yapıyoruz
                                             .Select(g => new
                                             {
                                                 SubCategoryName = g.FirstOrDefault().SubCategory.Name,  // İlk üründen alt kategori adını alıyoruz
                                                 ProductCount = g.Count()  // Ürün sayısını alıyoruz
                                             })
                                             .ToList();

            // Verileri JSON olarak döndürüyoruz
            var fullReport = new
            {
                CategoryReport = categoryReport,
                SubCategoryReport = subCategoryReport
            };

            return Json(fullReport);
        }

        public IActionResult Index()
        {
            var kritikStoklar = _context.ProductVariants
                .Where(pv => pv.Stock < 5)
                .Include(pv => pv.Product)
                .Include(pv => pv.Size)
                .Include(pv => pv.Color)
                .OrderBy(pv => pv.Stock)
                .ToList();

            // Grafik için veri hazırlama
            var chartData = kritikStoklar.Select(pv => new
            {
                ProductName = pv.Product.Name,
                Stock = pv.Stock
            }).ToList();

            ViewBag.ChartData = chartData;  // Bu veriyi View'e gönderiyoruz

            return View(kritikStoklar);
        }
        public IActionResult GetStockMovements(string filter)
        {
            // Filtreyi kontrol et ve uygun verileri al
            var stockMovements = _context.StockMovements.AsQueryable();

            // Şu anki tarih UTC formatına çevriliyor
            var todayUtc = DateTime.UtcNow.Date;

            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter)
                {
                    case "last7Days":
                        stockMovements = stockMovements.Where(sm => sm.Date >= DateTime.UtcNow.AddDays(-7));
                        break;
                    case "last30Days":
                        stockMovements = stockMovements.Where(sm => sm.Date >= DateTime.UtcNow.AddDays(-30));
                        break;
                    case "today":
                        stockMovements = stockMovements.Where(sm => sm.Date >= todayUtc && sm.Date < todayUtc.AddDays(1));
                        break;
                    default:
                        break;
                }
            }

            // ID'ye göre tersten sıralama
            stockMovements = stockMovements.OrderByDescending(sm => sm.Id);

            // Veriyi JSON formatında döndürüyoruz
            var model = stockMovements.Select(sm => new
            {
                ProductName = sm.ProductVariant.Product.Name,
                Date = sm.Date.ToLocalTime().ToString("dd-MM-yyyy HH:mm"),
                Quantity = sm.Quantity,
                MovementType = sm.MovementType,
                Description = sm.Description,
                CurrentStock = sm.CurrentStock
            }).ToList();

            return Json(model);
        }
        public async Task<IActionResult> GetStockByCategory()
        {
            var stockData = await _context.ProductVariants
                .GroupBy(pv => pv.Product.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalStock = g.Sum(pv => pv.Stock)
                })
                .ToListAsync();

            return Json(stockData);
        }
        public async Task<IActionResult> GetCriticalStockReport()
        {
            // Kritik stok eşiği (örneğin, 10 adet)
            var criticalStockThreshold = 5;

            var criticalStockData = await _context.ProductVariants
                .Where(pv => pv.Stock <= criticalStockThreshold) // Kritik stok eşiğinin altındaki ürünler
                .Select(pv => new
                {
                    ProductName = pv.Product.Name, // Ürün adı
                    CriticalStock = pv.Stock // Kritik stok miktarı
                })
                .ToListAsync();

            return Json(criticalStockData);
        }
        public async Task<IActionResult> GetTopSellingProduct()
        {
            var topProduct = await _context.OrderItems
                .GroupBy(oi => oi.ProductVariantId)
                .OrderByDescending(g => g.Count()) // En çok satılanı bul
                .Select(g => new
                {
                    ProductVariantId = g.Key,
                    SoldCount = g.Count(), // Satış miktarı
                    ProductName = g.First().ProductVariant.Product.Name,
                    Size = g.First().ProductVariant.Size.Name,
                    Color = g.First().ProductVariant.Color.Name
                })
                .FirstOrDefaultAsync();

            return Json(topProduct);
        }
        public async Task<IActionResult> GetDailySalesData()
        {
            var startOfDay = DateTime.Today;
            var endOfDay = DateTime.Today.AddDays(1).AddTicks(-1); // Bugünün son anı

            var todaySalesCount = await _context.StockMovements
                .Where(sm => sm.Description == "Ürün Satışı" && sm.Date >= startOfDay && sm.Date <= endOfDay)
                .CountAsync();

            // Günlük ciroyu hesapla (Bugün yapılan siparişlerin toplam fiyatı)
            var dailyRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startOfDay && o.OrderDate <= endOfDay)  // Bugün yapılan siparişler
                .SumAsync(o => o.TotalPrice);  // Siparişlerin toplam fiyatını topla

            // JSON olarak günlük satış sayısını ve ciroyu döndür
            return Json(new { DailySalesCount = todaySalesCount, DailyRevenue = dailyRevenue });
        }
        public IActionResult GetTotalSalesAndRevenue()
        {
            var salesQuery = _context.OrderItems.AsQueryable();

            var totalSales = salesQuery
                .GroupBy(oi => 1) // Tüm öğeleri grupluyoruz, çünkü toplam bir değer istiyoruz
                .Select(g => new
                {
                    TotalSoldQuantity = g.Sum(oi => oi.Quantity), // Toplam satılan ürün adedi
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price) // Toplam gelir (adedi x fiyatı)
                }).FirstOrDefault();

            return Json(totalSales);
        }


    }





}


