using etrade.Data;
using etrade.Models;
using etrade.Data.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Iyzipay;
using etrade.Areas.Shop.Services;
using Microsoft.AspNetCore.Http;
using etrade.Areas.Admin.Services;
using etrade.Areas.Tenant.Services;
using etrade.Areas.Tenant.Middlewares;
using etrade.Entity;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MySQL;
using etrade.Areas.Aysoft.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog yapılandırması
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.MySQL(
        connectionString: builder.Configuration.GetConnectionString("TenantConnection"),
        tableName: "Logs",
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog();

// Servislerin eklenmesi
builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// TenantContext konfigürasyonu
builder.Services.AddDbContext<TenantContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TenantConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
});

// Tenant Identity yapılandırması (AddIdentityCore kullanıyoruz)
builder.Services.AddIdentityCore<TenantUser>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
.AddRoles<TenantRole>()
.AddEntityFrameworkStores<TenantContext>()
.AddSignInManager<SignInManager<TenantUser>>() // BU SATIR EKLENDİ
.AddDefaultTokenProviders();

// TenantService kaydı
builder.Services.AddScoped<TenantService>();

// Esnek EtradeContext fabrikası
builder.Services.AddScoped<EtradeContext>(sp =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    if (httpContextAccessor.HttpContext?.Items["DbContext"] is EtradeContext dbContext)
    {
        logger.LogInformation("DbContext HttpContext'ten alındı");
        return dbContext;
    }

    logger.LogWarning("DbContext HttpContext'te bulunamadı, varsayılan bağlantı kullanılıyor");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
    optionsBuilder.UseMySql(configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 40)));

    return new EtradeContext(optionsBuilder.Options);
});

// Main Identity yapılandırması (EtradeContext için)
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
})
.AddEntityFrameworkStores<EtradeContext>()
.AddDefaultTokenProviders();

// Session ve diğer servisler
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication ayarları
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "MultiAuthSchemes";
    options.DefaultChallengeScheme = "MultiAuthSchemes";
})
.AddCookie("ShopCookie", options =>
{
    options.LoginPath = "/Shop/Account/Login";
    options.AccessDeniedPath = "/Shop/Account/AccessDenied";
    options.Cookie.Name = "ShopAuthCookie";
})
.AddCookie("AdminCookie", options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.Cookie.Name = "AdminAuthCookie";
})
.AddCookie("TenantCookie", options =>
{
    options.LoginPath = "/Tenant/Account/Login";
    options.AccessDeniedPath = "/Tenant/Account/AccessDenied";
    options.Cookie.Name = "TenantAuthCookie";
})
.AddCookie("CustomerCookie", options =>
{
    options.LoginPath = "/Customer/Account/Login";
    options.AccessDeniedPath = "/Customer/Account/AccessDenied";
    options.Cookie.Name = "CustomerAuthCookie";
})
.AddPolicyScheme("MultiAuthSchemes", "MultiAuthSchemes", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
            return "AdminCookie";
        else if (context.Request.Path.StartsWithSegments("/Shop"))
            return "ShopCookie";
        else if (context.Request.Path.StartsWithSegments("/Tenant"))
            return "TenantCookie";
        else if (context.Request.Path.StartsWithSegments("/Customer"))
            return "CustomerCookie";

        return "ShopCookie"; // Varsayılan
    };
});

// Diğer servisler
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<Iyzipay.Options>(_ => new Iyzipay.Options
{
    ApiKey = builder.Configuration["Iyzipay:ApiKey"],
    SecretKey = builder.Configuration["Iyzipay:SecretKey"],
    BaseUrl = builder.Configuration["Iyzipay:BaseUrl"]
});
builder.Services.AddSingleton<Iyzipay.HttpClient>();
builder.Services.AddSignalR();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CachedStoreSettingsService>();
builder.Services.AddSingleton<LicenseSettingsService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddHostedService<DiscountService>();
builder.Services.AddHostedService<CouponExpirationService>();


var app = builder.Build();

// Geliştirme ortamı ayarları
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// TenantMiddleware
app.UseMiddleware<TenantMiddleware>();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

// Route tanımlamaları
app.MapControllerRoute(
    name: "tenantRoute",
    pattern: "/{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/chathub");

app.Run();