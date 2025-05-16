using etrade.Data.Concrete;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class CategoryViewComponent : ViewComponent
{
    private readonly EtradeContext _context;

    public CategoryViewComponent(EtradeContext context)
    {
        _context = context;
    }

    public IViewComponentResult Invoke()
    {
        // Veritabanından Kategoriler ve Ürün Sayısı
        var categories = _context.Categories
            .Select(c => new
            {
                Id=c.Id,
                Name = c.Name,
                ImageUrl = c.ImageUrl ?? "/uploads/default.jpg", // Varsayılan resim
                ProductCount = c.Products.Count
            })
            .ToList();

        // Kategorileri ViewComponent'e gönderiyoruz
        return View(categories);
    }
}

