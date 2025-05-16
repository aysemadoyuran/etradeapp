using System;
using System.Linq;
using System.Security.Claims;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace etrade.Areas.Shop.Controllers
{
    [Area("Shop")]

    public class ProductController : Controller
    {
        private readonly EtradeContext _context;
        private readonly UserManager<AppUser> _userManager;


        public ProductController(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
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
            _userManager = userManager;

        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User); // Kullanıcı ID'sini alıyoruz
            var favoriteProductIds = _context.Favorites
                                              .Where(f => f.UserId == userId)
                                              .Select(f => f.ProductId)
                                              .ToList();
            ViewBag.FavoriteProductIds = favoriteProductIds;

            //SEPET KONTROLÜNÜ SİLDİK SORUN ÇIKARSA BURAYA BAK!!



            // Ürün verisini çekiyoruz

            var product = await _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // DTO'yu oluşturuyoruz
            var productDto = new ProductDetailDTO
            {
                Id = product.Id,
                Name = product.Name,
                ProductCode = product.ProductCode,
                Description = product.Description,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                CategoryName = product.Category?.Name,
                SubCategoryName = product.SubCategory?.Name,
                Images = _context.ColorImages
                    .Where(img => img.ProductId == product.Id)
                    .Select(img => new ImageDTO
                    {
                        ImageUrl = img.ImageUrl
                    }).ToList(),
                Variants = product.ProductVariants
            .GroupBy(v => new { v.ColorId, v.Color.Name, v.Color.ColorCode })
            .Select(g => new ColorVariantDTO
            {
                ColorId = g.Key.ColorId,
                ColorName = g.Key.Name,
                ColorCode = g.Key.ColorCode,
                Sizes = g.Select(v => new SizeDTO
                {
                    SizeId = v.SizeId,
                    SizeName = v.Size?.Name,
                    IsOutOfStock = v.Stock <= 0  // Stok durumu kontrolü
                }).ToList()
            }).ToList()
            };

            return View(productDto);
        }
        [HttpGet]
        public IActionResult GetImagesByColor(int productId, int colorId)
        {
            var images = _context.ColorImages
            .Where(pi => pi.ProductId == productId && pi.ColorId == colorId)
            .Select(pi => new
            {
                pi.Id,
                pi.ImageUrl
            })
            .ToList();

            if (images == null || images.Count == 0)
            {
                return NotFound("Görseller bulunamadı.");
            }
            return Json(images);
        }




        [HttpGet]
        public JsonResult GetSizesByColor(int productId, int colorId)
        {
            var sizes = _context.ProductVariants
                .Where(pv => pv.ColorId == colorId && pv.ProductId == productId)
                .Select(pv => new
                {
                    SizeId = pv.SizeId,
                    Name = pv.Size.Name,
                    Stock = pv.Stock  // Stock bilgisini de dahil ediyoruz
                })
                .Distinct()
                .ToList();

            if (sizes == null || sizes.Count == 0)
            {
                Console.WriteLine("Hiç bir beden verisi bulunamadı!");
            }
            else
            {
                Console.WriteLine($"Veri bulundu: {sizes.Count} beden");
            }

            return Json(sizes);
        }
        public async Task<IActionResult> List(int[] categoryIds, int[] subCategoryIds, int[] colorIds, int[] sizeIds, string sortOrder)
        {
            var query = _context.Products
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Size)
                .Include(p => p.Category)
                .Include(p => p.SubCategory)
                .Where(p => p.IsActive) // Sadece aktif ürünleri getiriyoruz
                .AsQueryable();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favoriteProductIds = _context.Favorites
                                              .Where(f => f.UserId == userId)
                                              .Select(f => f.ProductId)
                                              .ToList();

            // **İndirimli Ürünler Kategorisi için kontrol ekle**
            if (categoryIds != null && categoryIds.Length > 0)
            {
                // İndirimli Ürünler kategorisini kontrol ediyoruz
                if (categoryIds.Contains(-1))  // -1, İndirimli Ürünler'in özel id'si olabilir
                {
                    // DiscountId'si null olmayan ürünleri filtrele
                    query = query.Where(p => p.DiscountId != null);
                    categoryIds = categoryIds.Where(id => id != -1).ToArray();  // -1'yi liste dışı bırak
                }

                // Diğer kategori filtreleme işlemleri
                if (categoryIds.Length > 0)
                {
                    query = query.Where(p => categoryIds.Contains(p.CategoryId));
                }
            }

            if (subCategoryIds != null && subCategoryIds.Length > 0)
                query = query.Where(p => p.SubCategoryId.HasValue && subCategoryIds.Contains(p.SubCategoryId.Value));

            if (colorIds != null && colorIds.Length > 0)
                query = query.Where(p => p.ProductVariants.Any(v => colorIds.Contains(v.ColorId)));

            if (sizeIds != null && sizeIds.Length > 0)
                query = query.Where(p => p.ProductVariants.Any(v => sizeIds.Contains(v.SizeId)));

            // **Sıralama Mantığı**
            switch (sortOrder)
            {
                case "a-z":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "z-a":
                    query = query.OrderByDescending(p => p.Name);
                    break;
                case "price-low-high":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price-high-low":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt); // Varsayılan sıralama: En yeni ürünler
                    break;
            }

            var products = await query
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    OriginalPrice = p.OriginalPrice,
                    ProductCode = p.ProductCode,
                    Description = p.Description,
                    CommentCount=p.CommentCount,
                    AverageRating=p.AverageRating,
                    ImageUrls = _context.ColorImages
                        .Where(img => img.ProductId == p.Id)
                        .Select(img => img.ImageUrl)
                        .ToList(),
                    Variants = p.ProductVariants.Select(v => new ProductVariantDto
                    {
                        SizeId = v.SizeId,
                        SizeName = v.Size.Name,
                        ColorName = v.Color.Name,
                        Color = new ColorDto
                        {
                            Id = v.Color.Id,
                            Name = v.Color.Name,
                            ColorCode = v.Color.ColorCode
                        },
                        ImageUrls = _context.ColorImages
                            .Where(img => img.ProductId == p.Id && img.ColorId == v.ColorId)
                            .Select(img => img.ImageUrl)
                            .ToList()
                    }).ToList()
                })
                .ToListAsync();

            Console.WriteLine($"Filtrelenen ürün sayısı: {products.Count}");

            var categories = await _context.Categories.ToListAsync();
            var subCategories = await _context.SubCategories.ToListAsync();

            var categorySubCategoryMap = categories.ToDictionary(
                category => category.Id,
                category => subCategories.Where(sc => sc.CategoryId == category.Id).ToList()
            );

            // İndirimli Ürünler Kategorisini ViewData'ya ekliyoruz
            var discountedCategory = new Category
            {
                Id = -1,
                Name = "İndirimli Ürünler",
                ImageUrl = "/uploads/discount.jpg"  // İndirimli ürünler için özel bir resim
            };
            categories.Insert(0, discountedCategory);  // İndirimli Ürünler kategorisini başa ekliyoruz

            ViewData["Categories"] = categories;
            ViewData["CategorySubCategoryMap"] = categorySubCategoryMap;
            ViewData["Colors"] = await _context.Colors.ToListAsync();
            ViewData["Sizes"] = await _context.Sizes.ToListAsync();
            ViewData["SelectedSortOrder"] = sortOrder;
            ViewBag.FavoriteProductIds = favoriteProductIds;

            return View(products);
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = "ShopCookie")]
        public IActionResult AddToCart(int productId, int colorId, int sizeId, int quantity, decimal price)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return Redirect($"/Shop/Product/Details?id={productId}");
            }

            // Sepet var mı kontrolü
            var cart = _context.Baskets.FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                TempData["ErrorMessage"] = "Sepet bulunamadı.";
                return Redirect($"/Shop/Product/Details?id={productId}");
            }

            var basketId = cart.Id;

            // Ürün varyantını bul
            var productVariant = _context.ProductVariants
                .FirstOrDefault(pv => pv.ProductId == productId && pv.ColorId == colorId && pv.SizeId == sizeId);

            if (productVariant == null)
            {
                TempData["ErrorMessage"] = "Geçerli ürün varyantı bulunamadı.";
                return Redirect($"/Shop/Product/Details?id={productId}");
            }
            var IsOutOfStock = productVariant.Stock >= quantity;
            if (IsOutOfStock)
            {
                // Sepetteki mevcut ürünü alıyoruz
                var existingProduct = _context.ItemsBaskets
                    .FirstOrDefault(item => item.BasketId == basketId && item.ProductVariantId == productVariant.Id);

                // Mevcut sepetteki ürünün adedini alıyoruz
                int currentQuantity = existingProduct != null ? existingProduct.Quantity : 0;

                // Yeni eklemeye çalıştığımız ürünle birlikte toplam adedi hesaplıyoruz
                int totalQuantity = currentQuantity + quantity;

                // Eğer toplam miktar stok miktarını aşarsa hata mesajı
                if (totalQuantity > productVariant.Stock)
                {
                    TempData["ErrorMessage"] = $"Yetersiz Stok Miktarı! Mevcut stok: {productVariant.Stock}";
                }
                else
                {
                    // Sepette zaten varsa, adeti artırıyoruz
                    if (existingProduct != null)
                    {
                        existingProduct.Quantity += quantity;
                        existingProduct.TotalPrice = existingProduct.Quantity * price;
                    }
                    else
                    {
                        // Sepette ürün yoksa, yeni ürün ekliyoruz
                        var newCartItem = new ItemsBasket
                        {
                            ProductVariantId = productVariant.Id,
                            BasketId = basketId,
                            Quantity = quantity,
                            TotalPrice = price * quantity
                        };

                        _context.ItemsBaskets.Add(newCartItem);
                    }

                    // Değişiklikleri kaydediyoruz
                    _context.SaveChanges();
                    TempData["SuccessMessage"] = "Ürün sepete eklendi.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Yetersiz Stok Miktarı";
            }

            return Redirect($"/Shop/Product/Details?id={productId}");
        }
        public IActionResult GetInactiveBasketItemCount()
        {
            // Kullanıcı ID'sini al
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Kullanıcı bilgisi bulunamadı.");
            }

            // Kullanıcıya ait aktif olmayan öğeleri say
            var basket = _context.Baskets
                                 .Where(b => b.UserId == userId)
                                 .FirstOrDefault();

            if (basket == null)
            {
                return BadRequest("Kullanıcıya ait sepet bulunamadı.");
            }

            var inactiveItemsCount = _context.ItemsBaskets
                                              .Where(item => item.BasketId == basket.Id && item.IsActive == false)
                                              .Count();

            return Ok(inactiveItemsCount);
        }
        [HttpPost]
        [Authorize(AuthenticationSchemes = "ShopCookie")]
        public IActionResult ToggleFavorite(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorite = _context.Favorites.FirstOrDefault(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null)
            {
                // Ürün favorilerde yoksa ekle
                _context.Favorites.Add(new Favorite { UserId = userId, ProductId = productId });
            }
            else
            {
                // Ürün favorilerde varsa çıkar
                _context.Favorites.Remove(favorite);
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }
        [HttpGet]
        [Authorize(AuthenticationSchemes = "ShopCookie")]
        public IActionResult GetFavoriteProductIds()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favoriteProductIds = _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToList();

            return Json(new { favoriteProductIds = favoriteProductIds });
        }








    }

}


