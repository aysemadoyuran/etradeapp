using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using static etrade.Areas.Tenant.Controllers.StoreController;
using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace etrade.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    [Authorize(AuthenticationSchemes = "TenantCookie")]

    public class RequestController : Controller
    {
        private readonly ILogger<RequestController> _logger;
        private readonly TenantContext _context;


        public RequestController(ILogger<RequestController> logger, TenantContext context)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetCancellationRequests()
        {
            var requests = await _context.CancellationRequests
                .Include(r => r.License)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new
                {
                    r.Id,
                    r.LicenseId,
                    r.Reason,
                    RequestDate = r.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                    r.IsApproved,
                    r.IsCompleted,
                    Domain = _context.TenantStores
                        .Where(ts => ts.LicenseId == r.LicenseId)
                        .Select(ts => ts.Domain)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(requests);
        }
        [HttpGet]
        public async Task<IActionResult> GetCancellationRequestDetails(int id)
        {
            var request = await _context.CancellationRequests
                .Include(r => r.License)
                    .ThenInclude(l => l.Payments) // License'a ait Payments'ı dahil ediyoruz
                .Include(r => r.License)
                    .ThenInclude(l => l.TenantStores) // License'a ait TenantStore'ları dahil ediyoruz
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound("İptal talebi bulunamadı.");

            // License Payments bilgilerini alıyoruz
            var licensePayments = request.License.Payments.Select(p => new
            {
                p.Id,
                p.Price,
                p.StartPeriod,
                p.EndPeriod,
                p.IsPaid // Örnek olarak diğer gerekli alanları alabilirsiniz
            }).ToList();

            // TenantStore bilgilerini alıyoruz
            var tenantStores = request.License.TenantStores.Select(ts => new
            {
                ts.Id,
                ts.StoreName,
                ts.Email,
                ts.ConnectionString,
                ts.Domain, // Mağaza domaini
                ts.LogoUrl
            }).ToList();

            var response = new
            {
                request.Id,
                request.LicenseId,
                request.Reason,
                RequestDate = request.RequestDate.ToString("yyyy-MM-dd HH:mm"),
                request.IsApproved,
                request.IsCompleted,

                // License Payments bilgileri
                LicensePayments = licensePayments,

                // TenantStore bilgileri
                TenantStores = tenantStores
            };

            return Json(response);
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult RequestDetail()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ApproveCancellationRequest([FromBody] int id)
        {
            var request = _context.CancellationRequests.FirstOrDefault(r => r.Id == id);
            if (request == null) return NotFound("Talep bulunamadı.");

            if (request.IsApproved)
                return BadRequest("Talep zaten onaylanmış.");

            request.IsApproved = true;
            request.ApprovalDate = DateTime.Now;
            _context.SaveChanges();

            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> CompleteProcess(int id)
        {
            try
            {
                // İlgili talep kaydını al
                var request = await _context.CancellationRequests
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    return Json(new { success = false, message = "Talep bulunamadı." });
                }

                // Talep onaylandığında IsCompleted'i true yap
                request.IsCompleted = true;

                // Talep kaydını güncelle
                _context.CancellationRequests.Update(request);

                // Değişiklikleri veritabanına kaydet
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Veriler mail olarak gönderildi ve işlem tamamlandı." });
            }
            catch (Exception ex)
            {
                // Hata durumu
                return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerileriHazirlaVeExcelYap([FromBody] int id)
        {
            try
            {
                // 1. LicenseId'yi bul
                var licenseId = await _context.CancellationRequests
                    .Where(r => r.Id == id)
                    .Select(r => r.LicenseId)
                    .FirstOrDefaultAsync();

                if (licenseId == 0)
                    return NotFound("Geçerli bir lisans bulunamadı.");

                // 2. TenantStore'u bul
                var store = await _context.TenantStores
                    .FirstOrDefaultAsync(ts => ts.LicenseId == licenseId);

                if (store == null)
                    return NotFound("TenantStore kaydı bulunamadı.");

                // 3. Şifreli connection string'i çöz
                var decryptedConnStr = EncryptionHelper.Decrypt(store.ConnectionString);

                // 4. Verileri paralel olarak çek
                var tasks = new List<Task>
        {
            FetchOrdersFromEtradeContext(decryptedConnStr),
            FetchRefundsFromEtradeContext(decryptedConnStr),
            FetchProductsFromEtradeContext(decryptedConnStr)
        };

                await Task.WhenAll(tasks);
                var orders = ((Task<List<Order>>)tasks[0]).Result;
                var refunds = ((Task<List<RefundRequest>>)tasks[1]).Result;
                var products = ((Task<List<Product>>)tasks[2]).Result;

                // 5. Excel dosyasını oluştur
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();

                // Sayfaları oluştur
                GenerateOrdersWorksheet(package, orders);
                GenerateRefundsWorksheet(package, refunds);
                GenerateProductsWorksheet(package, products);

                // 6. Excel dosyasını döndür
                var fileName = $"TamRapor_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel rapor oluşturulurken hata oluştu. ID: {Id}", id);
                return StatusCode(500, "Excel rapor oluşturulurken bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        private async Task<List<Order>> FetchOrdersFromEtradeContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            using var context = new EtradeContext(optionsBuilder.Options);

            return await context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                .Include(o => o.ShippingAddress)
                    .ThenInclude(a => a.Il)
                .Include(o => o.ShippingAddress)
                    .ThenInclude(a => a.Ilce)
                .Include(o => o.ShippingAddress)
                    .ThenInclude(a => a.District)
                .Include(o => o.ShippingAddress)
                    .ThenInclude(a => a.Street)
                .Include(o => o.PaymentMethod)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        private async Task<List<RefundRequest>> FetchRefundsFromEtradeContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            using var context = new EtradeContext(optionsBuilder.Options);

            return await context.RefundRequests
                .AsNoTracking()
                .Include(r => r.Order)
                    .ThenInclude(o => o.User)
                .Include(r => r.PaymentMethod)
                .Include(r => r.RefundedItems)
                    .ThenInclude(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(r => r.RefundedItems)
                    .ThenInclude(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Color)
                .Include(r => r.RefundedItems)
                    .ThenInclude(ri => ri.ProductVariant)
                        .ThenInclude(pv => pv.Size)
                .OrderByDescending(r => r.RefundRequestDate)
                .ToListAsync();
        }


        private async Task<List<Product>> FetchProductsFromEtradeContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            using var context = new EtradeContext(optionsBuilder.Options);

            return await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Size)
                .Include(p => p.Discount)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        private string RemoveHtmlTags(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        private void GenerateProductsWorksheet(ExcelPackage package, List<Product> products)
        {
            var worksheet = package.Workbook.Worksheets.Add("Ürünler");

            // Stil ayarları
            var styles = new
            {
                HeaderColor = System.Drawing.Color.FromArgb(31, 78, 121),
                AlternateRowColor = System.Drawing.Color.FromArgb(241, 241, 241),
                BorderColor = System.Drawing.Color.FromArgb(200, 200, 200),
                TotalRowColor = System.Drawing.Color.FromArgb(198, 224, 180)
            };

            // Doküman özellikleri
            package.Workbook.Properties.Title = "Ürün Raporu";
            package.Workbook.Properties.Author = "E-Ticaret Sistemi";

            // Başlıklar
            var headers = new[] {
        ("Temel Bilgiler", 7),  // 4 yerine 7
        ("Kategori Bilgileri", 3),
        ("Varyant Bilgileri", 5),
        ("Ekstra Bilgiler", 3)
    };

            var subHeaders = new[] {
        "Ürün Kodu", "Ürün Adı", "Oluşturma Tarihi", "Güncelleme Tarihi",
        "Aktif Mi?", "Tamamlandı Mı?", "Yorum Sayısı", "Ortalama Puan",
        "Ana Kategori", "Alt Kategori",
        "Renk", "Beden", "Stok", "Varyant Sayısı", "Toplam Stok"
    };

            // Ana başlıklar
            int col = 1;
            foreach (var (header, span) in headers)
            {
                worksheet.Cells[1, col, 1, col + span - 1].Merge = true;
                worksheet.Cells[1, col].Value = header;
                col += span;
            }

            // Alt başlıklar
            for (int i = 0; i < subHeaders.Length; i++)
            {
                worksheet.Cells[2, i + 1].Value = subHeaders[i];
            }

            // Stil uygula
            ApplyWorksheetStyles(worksheet, subHeaders.Length, styles);

            // Sütun genişlikleri
            int[] columnWidths = { 20, 30, 40, 20, 20, 15, 15, 15, 15, 15, 20, 20, 20, 15, 15, 15, 15, 15, 20, 15, 20 };
            for (int i = 0; i < columnWidths.Length; i++)
            {
                worksheet.Column(i + 1).Width = columnWidths[i];
            }

            // Veri satırları
            int row = 3;
            foreach (var product in products)
            {
                // Temel Bilgiler
                worksheet.Cells[row, 1].Value = product.ProductCode;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.CreatedAt.ToString("dd.MM.yyyy HH:mm") ?? "Bilgi Yok"; // Oluşturma tarihi kontrolü
                worksheet.Cells[row, 4].Value = product.UpdatedAt.ToString("dd.MM.yyyy HH:mm") ?? "Bilgi Yok"; // Güncelleme tarihi kontrolü
                worksheet.Cells[row, 5].Value = product.IsActive ? "Evet" : "Hayır";
                worksheet.Cells[row, 6].Value = product.Complete ? "Evet" : "Hayır";
                worksheet.Cells[row, 7].Value = product.CommentCount;
                worksheet.Cells[row, 8].Value = product.AverageRating;

                // Kategori Bilgileri
                worksheet.Cells[row, 9].Value = product.Category?.Name;
                worksheet.Cells[row, 10].Value = product.SubCategory?.Name;

                // Varyant Bilgileri
                var variants = product.ProductVariants?.ToList() ?? new List<ProductVariant>();
                var colors = variants.Select(v => v.Color?.Name).Distinct().Where(c => !string.IsNullOrEmpty(c));
                var sizes = variants.Select(v => v.Size?.Name).Distinct().Where(s => !string.IsNullOrEmpty(s));
                var totalStock = variants.Sum(v => v.Stock);

                worksheet.Cells[row, 11].Value = string.Join(", ", colors);
                worksheet.Cells[row, 12].Value = string.Join(", ", sizes);
                worksheet.Cells[row, 13].Value = totalStock;
                worksheet.Cells[row, 14].Value = variants.Count;
                worksheet.Cells[row, 15].Value = totalStock;

                // Alternatif satır rengi
                if (row % 2 == 0)
                {
                    worksheet.Row(row).Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Row(row).Style.Fill.BackgroundColor.SetColor(styles.AlternateRowColor);
                }

                row++;
            }

            // Sayı formatları
            worksheet.Column(9).Style.Numberformat.Format = "#,##0.00 ₺";  // Yorum sayısı ve diğer sayılarla ilgili formatlar
            worksheet.Column(10).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(11).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(12).Style.Numberformat.Format = "%0";

            // Sayfa ayarlarını uygula
            FinalizeWorksheet(worksheet, row, subHeaders.Length, products.Count,
                $"Toplam {products.Count} Ürün - {products.Sum(p => p.ProductVariants?.Sum(v => v.Stock) ?? 0)} Toplam Stok",
                styles.TotalRowColor);
        }

        private void GenerateOrdersWorksheet(ExcelPackage package, List<Order> orders)
        {
            var worksheet = package.Workbook.Worksheets.Add("Siparişler");

            // Professional color scheme
            var styles = new
            {
                HeaderColor = System.Drawing.Color.FromArgb(31, 78, 121),
                AlternateRowColor = System.Drawing.Color.FromArgb(241, 241, 241),
                BorderColor = System.Drawing.Color.FromArgb(200, 200, 200),
                TotalRowColor = System.Drawing.Color.FromArgb(198, 224, 180)
            };

            // Set document properties
            package.Workbook.Properties.Title = "Sipariş Raporu";
            package.Workbook.Properties.Author = "E-Ticaret Sistemi";
            package.Workbook.Properties.Company = "Şirket Adı";

            // Headers with grouped columns
            string[] headers = {
        "Sipariş Bilgileri", "", "", "", "", "", "",
        "Ödeme Bilgileri", "", "", "", "",
        "Ürün Bilgileri", "", "", "", "",
        "Teslimat Bilgileri", "", "", "", "", "", "", ""
    };

            string[] subHeaders = {
        "Sipariş Kodu", "Müşteri Adı", "Toplam Tutar", "Durum", "Sipariş Tarihi",
        "Ödeme Durumu", "Güncelleme Tarihi", "Ödeme Yöntemi", "Ödeme Tutarı",
        "Ödeme Tarihi", "Ödeme Türü", "Ödeme Token", "Ürün Adı", "Miktar", "Birim Fiyat",
        "Toplam Fiyat", "Renk", "Beden", "Teslimat Adı", "Adres",
        "Telefon", "Adres Başlığı", "İl", "İlçe", "Semt", "Mahalle"
    };

            // Merge header cells for grouping
            worksheet.Cells[1, 1, 1, 7].Merge = true; // Sipariş Bilgileri
            worksheet.Cells[1, 8, 1, 12].Merge = true; // Ödeme Bilgileri
            worksheet.Cells[1, 13, 1, 19].Merge = true; // Ürün Bilgileri
            worksheet.Cells[1, 20, 1, 26].Merge = true; // Teslimat Bilgileri

            // Main headers
            worksheet.Cells[1, 1].Value = "Sipariş Bilgileri";
            worksheet.Cells[1, 8].Value = "Ödeme Bilgileri";
            worksheet.Cells[1, 13].Value = "Ürün Bilgileri";
            worksheet.Cells[1, 20].Value = "Teslimat Bilgileri";

            // Sub headers
            for (int i = 0; i < subHeaders.Length; i++)
            {
                worksheet.Cells[2, i + 1].Value = subHeaders[i];
            }

            // Stil uygula
            ApplyWorksheetStyles(worksheet, subHeaders.Length, styles);

            // Column widths
            int[] columnWidths = { 18, 25, 15, 15, 20, 15, 20, 20, 15, 20, 15, 25, 30, 10, 15, 15, 15, 15, 25, 40, 15, 20, 15, 15, 15, 15 };
            for (int i = 0; i < columnWidths.Length; i++)
            {
                worksheet.Column(i + 1).Width = columnWidths[i];
            }

            // Data rows
            int row = 3;
            var groupedOrders = orders.GroupBy(o => o.OrderCode).OrderByDescending(g => g.First().OrderDate);

            foreach (var orderGroup in groupedOrders)
            {
                var firstOrder = orderGroup.First();
                int groupStartRow = row;
                bool isFirstItem = true;

                foreach (var orderItem in firstOrder.OrderItems ?? Enumerable.Empty<OrderItem>())
                {
                    // Order info (only in first row of group)
                    if (isFirstItem)
                    {
                        worksheet.Cells[row, 1].Value = firstOrder.OrderCode;
                        worksheet.Cells[row, 2].Value = firstOrder.User?.FullName;
                        worksheet.Cells[row, 3].Value = firstOrder.TotalPrice;
                        worksheet.Cells[row, 4].Value = firstOrder.OrderStatus;
                        worksheet.Cells[row, 5].Value = firstOrder.OrderDate?.ToString("dd.MM.yyyy HH:mm");
                        worksheet.Cells[row, 6].Value = firstOrder.PaymentStatus;
                        worksheet.Cells[row, 7].Value = firstOrder.UpdateDate?.ToString("dd.MM.yyyy HH:mm");

                        // Payment info
                        if (firstOrder.PaymentMethod != null)
                        {
                            worksheet.Cells[row, 8].Value = firstOrder.PaymentMethod.PaymentMethodType;
                            worksheet.Cells[row, 9].Value = firstOrder.PaymentMethod.Amount;
                            worksheet.Cells[row, 10].Value = firstOrder.PaymentMethod.PaymentDate.ToString("dd.MM.yyyy HH:mm");
                            worksheet.Cells[row, 11].Value = firstOrder.PaymentMethod.PaymentStatus;
                            worksheet.Cells[row, 12].Value = firstOrder.PaymentMethod.PaymentToken;
                        }

                        // Address info
                        var address = firstOrder.ShippingAddress;
                        worksheet.Cells[row, 19].Value = address?.NameSurname;
                        worksheet.Cells[row, 20].Value = address?.AcikAdres;
                        worksheet.Cells[row, 21].Value = address?.Telefon;
                        worksheet.Cells[row, 22].Value = address?.Title;
                        worksheet.Cells[row, 23].Value = address?.Il?.Ad;
                        worksheet.Cells[row, 24].Value = address?.Ilce?.Ad;
                        worksheet.Cells[row, 25].Value = address?.District?.SemtAdi;
                        worksheet.Cells[row, 26].Value = address?.Street?.MahalleAdi;
                    }

                    // Product info
                    worksheet.Cells[row, 13].Value = orderItem?.ProductVariant?.Product?.Name;
                    worksheet.Cells[row, 14].Value = orderItem?.Quantity;
                    worksheet.Cells[row, 15].Value = orderItem?.Price;
                    worksheet.Cells[row, 16].Value = orderItem?.TotalPrice;
                    worksheet.Cells[row, 17].Value = orderItem?.ProductVariant?.Color?.Name;
                    worksheet.Cells[row, 18].Value = orderItem?.ProductVariant?.Size?.Name;

                    // Alternate row coloring
                    if (row % 2 == 0)
                    {
                        worksheet.Row(row).Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Row(row).Style.Fill.BackgroundColor.SetColor(styles.AlternateRowColor);
                    }

                    row++;
                    isFirstItem = false;
                }

                // Group styling
                if (groupStartRow <= row - 1)
                {
                    var groupRange = worksheet.Cells[groupStartRow, 1, row - 1, 26];

                    // Borders
                    groupRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Top.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Bottom.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Left.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Right.Color.SetColor(styles.BorderColor);

                    // Highlight first row of each group
                    var firstRowRange = worksheet.Cells[groupStartRow, 1, groupStartRow, 26];
                    firstRowRange.Style.Font.Bold = true;
                    firstRowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    firstRowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(221, 235, 247));
                }

                // Add empty row between groups
                row++;
            }

            // Number formatting
            worksheet.Column(3).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(9).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(15).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(16).Style.Numberformat.Format = "#,##0.00 ₺";

            // Sayfa ayarlarını uygula
            FinalizeWorksheet(worksheet, row, subHeaders.Length, orders.Count,
                $"Toplam {orders.Count} Sipariş - {orders.Sum(o => o.TotalPrice):#,##0.00 ₺}",
                styles.TotalRowColor);
        }

        private void GenerateRefundsWorksheet(ExcelPackage package, List<RefundRequest> refunds)
        {
            var worksheet = package.Workbook.Worksheets.Add("İadeler");

            // Professional color scheme
            var styles = new
            {
                HeaderColor = System.Drawing.Color.FromArgb(31, 78, 121),
                AlternateRowColor = System.Drawing.Color.FromArgb(241, 241, 241),
                BorderColor = System.Drawing.Color.FromArgb(200, 200, 200),
                TotalRowColor = System.Drawing.Color.FromArgb(198, 224, 180)
            };

            // Set document properties
            package.Workbook.Properties.Title = "İade Raporu";
            package.Workbook.Properties.Author = "E-Ticaret Sistemi";
            package.Workbook.Properties.Company = "Şirket Adı";

            // Headers with grouped columns
            string[] headers = {
        "İade Bilgileri", "", "", "", "", "",
        "Ödeme Bilgileri", "", "", "",
        "Ürün Bilgileri", "", "", "", "", "", ""
    };

            string[] subHeaders = {
        "İade Kodu", "Sipariş Kodu", "Müşteri Adı", "Toplam Tutar", "Durum", "Talep Tarihi",
        "Ödeme Yöntemi", "IBAN", "Ad Soyad", "Ödeme Durumu",
        "Ürün Adı", "Miktar", "Birim Fiyat", "Toplam Fiyat", "Renk", "Beden", "İade Nedeni"
    };

            // Merge header cells for grouping
            worksheet.Cells[1, 1, 1, 6].Merge = true; // İade Bilgileri
            worksheet.Cells[1, 7, 1, 10].Merge = true; // Ödeme Bilgileri
            worksheet.Cells[1, 11, 1, 17].Merge = true; // Ürün Bilgileri

            // Main headers
            worksheet.Cells[1, 1].Value = "İade Bilgileri";
            worksheet.Cells[1, 7].Value = "Ödeme Bilgileri";
            worksheet.Cells[1, 11].Value = "Ürün Bilgileri";

            // Sub headers
            for (int i = 0; i < subHeaders.Length; i++)
            {
                worksheet.Cells[2, i + 1].Value = subHeaders[i];
            }

            // Stil uygula
            ApplyWorksheetStyles(worksheet, subHeaders.Length, styles);

            // Column widths
            int[] columnWidths = { 20, 20, 25, 15, 15, 20, 20, 25, 25, 15, 30, 10, 15, 15, 15, 15, 20 };
            for (int i = 0; i < columnWidths.Length; i++)
            {
                worksheet.Column(i + 1).Width = columnWidths[i];
            }

            // Data rows
            int row = 3;
            foreach (var refund in refunds.OrderByDescending(r => r.RefundRequestDate))
            {
                int groupStartRow = row;
                bool isFirstItem = true;

                foreach (var refundItem in refund.RefundedItems ?? Enumerable.Empty<RefundedItem>())
                {
                    // İade bilgileri (sadece grup içindeki ilk satırda)
                    if (isFirstItem)
                    {
                        worksheet.Cells[row, 1].Value = refund.RefundCode;
                        worksheet.Cells[row, 2].Value = refund.Order?.OrderCode;
                        worksheet.Cells[row, 3].Value = refund.Order?.User?.FullName;
                        worksheet.Cells[row, 4].Value = refund.TotalPrice;
                        worksheet.Cells[row, 5].Value = refund.RefundStatus;
                        worksheet.Cells[row, 6].Value = refund.RefundRequestDate.ToString("dd.MM.yyyy HH:mm");

                        // Ödeme bilgileri
                        if (refund.PaymentMethod != null)
                        {
                            worksheet.Cells[row, 7].Value = refund.PaymentMethod.PaymentMethodType;
                            worksheet.Cells[row, 8].Value = refund.Iban;
                            worksheet.Cells[row, 9].Value = refund.fullName;
                            worksheet.Cells[row, 10].Value = refund.PaymentMethod.PaymentStatus;
                        }
                    }

                    // Ürün bilgileri
                    if (refundItem.ProductVariant != null)
                    {
                        worksheet.Cells[row, 11].Value = refundItem.ProductVariant.Product?.Name;
                        worksheet.Cells[row, 12].Value = refundItem.Quantity;
                        worksheet.Cells[row, 13].Value = refundItem.TotalPrice / refundItem.Quantity; // Birim fiyat
                        worksheet.Cells[row, 14].Value = refundItem.TotalPrice;
                        worksheet.Cells[row, 15].Value = refundItem.ProductVariant.Color?.Name;
                        worksheet.Cells[row, 16].Value = refundItem.ProductVariant.Size?.Name;
                        worksheet.Cells[row, 17].Value = refundItem.ReasonType;
                    }

                    // Alternate row coloring
                    if (row % 2 == 0)
                    {
                        worksheet.Row(row).Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Row(row).Style.Fill.BackgroundColor.SetColor(styles.AlternateRowColor);
                    }

                    row++;
                    isFirstItem = false;
                }

                // Group styling
                if (groupStartRow <= row - 1)
                {
                    var groupRange = worksheet.Cells[groupStartRow, 1, row - 1, 17];

                    // Borders
                    groupRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    groupRange.Style.Border.Top.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Bottom.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Left.Color.SetColor(styles.BorderColor);
                    groupRange.Style.Border.Right.Color.SetColor(styles.BorderColor);

                    // Highlight first row of each group
                    var firstRowRange = worksheet.Cells[groupStartRow, 1, groupStartRow, 17];
                    firstRowRange.Style.Font.Bold = true;
                    firstRowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    firstRowRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(221, 235, 247));
                }

                // Add empty row between groups
                row++;
            }

            // Number formatting
            worksheet.Column(4).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(13).Style.Numberformat.Format = "#,##0.00 ₺";
            worksheet.Column(14).Style.Numberformat.Format = "#,##0.00 ₺";

            // Sayfa ayarlarını uygula
            FinalizeWorksheet(worksheet, row, subHeaders.Length, refunds.Count,
                $"Toplam {refunds.Count} İade - {refunds.Sum(r => r.TotalPrice):#,##0.00 ₺}",
                styles.TotalRowColor);
        }

        private void ApplyWorksheetStyles(ExcelWorksheet worksheet, int columnCount, dynamic styles)
        {
            // Ana başlık stili
            using (var headerRange = worksheet.Cells[1, 1, 1, columnCount])
            {
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.Size = 12;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(styles.HeaderColor);
                headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Alt başlık stili
            using (var subHeaderRange = worksheet.Cells[2, 1, 2, columnCount])
            {
                subHeaderRange.Style.Font.Bold = true;
                subHeaderRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                subHeaderRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                subHeaderRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                subHeaderRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                subHeaderRange.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
                subHeaderRange.Style.Border.Bottom.Color.SetColor(styles.HeaderColor);
            }
        }

        private void FinalizeWorksheet(ExcelWorksheet worksheet, int dataEndRow, int columnCount,
            int itemCount, string summaryText, System.Drawing.Color totalRowColor)
        {
            // Toplam satırı
            var totalRange = worksheet.Cells[dataEndRow, 1, dataEndRow, columnCount];
            totalRange.Merge = true;
            totalRange.Value = summaryText;
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Font.Size = 12;
            totalRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            totalRange.Style.Fill.BackgroundColor.SetColor(totalRowColor);
            totalRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            totalRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Otomatik filtre
            worksheet.Cells[2, 1, dataEndRow - 1, columnCount].AutoFilter = true;

            // Sayfa ayarları
            worksheet.View.FreezePanes(3, 1);
            worksheet.PrinterSettings.RepeatRows = new ExcelAddress("1:2");
            worksheet.PrinterSettings.PaperSize = ePaperSize.A4;
            worksheet.PrinterSettings.Orientation = eOrientation.Landscape;
            worksheet.PrinterSettings.FitToPage = true;
            worksheet.PrinterSettings.FitToWidth = 1;
            worksheet.PrinterSettings.HorizontalCentered = true;
            worksheet.HeaderFooter.OddFooter.CenteredText = "&D &T - Sayfa &P / &N";
        }





    }

}
