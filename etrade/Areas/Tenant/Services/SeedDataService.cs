using etrade.Areas.Tenant.Services;
using etrade.Data.Concrete;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;

public class SeedDataService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly TenantService _tenantService;
    private readonly IWebHostEnvironment _env;



    public SeedDataService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        TenantService tenantService,
        IServiceProvider serviceProvider,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
        _serviceProvider = serviceProvider;
        _env = env;

    }

    // Rolleri seed et
    public async Task SeedRolesAsync(int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        // Admin rolünü ekle
        if (!await RoleExistsAsync(connectionString, "admin"))
        {
            var adminRole = new AppRole { Name = "admin" };
            var result = await CreateRoleAsync(connectionString, adminRole);
            if (result)
            {
                Console.WriteLine($"Admin rolü (Tenant ID: {tenantId}) başarıyla oluşturuldu.");
            }
            else
            {
                Console.WriteLine($"Admin rolü (Tenant ID: {tenantId}) oluşturulurken hata oluştu.");
            }
        }
        else
        {
            Console.WriteLine($"Admin rolü (Tenant ID: {tenantId}) zaten mevcut.");
        }

        // Customer rolünü ekle
        if (!await RoleExistsAsync(connectionString, "customer"))
        {
            var customerRole = new AppRole { Name = "customer" };
            var result = await CreateRoleAsync(connectionString, customerRole);
            if (result)
            {
                Console.WriteLine($"Customer rolü (Tenant ID: {tenantId}) başarıyla oluşturuldu.");
            }
            else
            {
                Console.WriteLine($"Customer rolü (Tenant ID: {tenantId}) oluşturulurken hata oluştu.");
            }
        }
        else
        {
            Console.WriteLine($"Customer rolü (Tenant ID: {tenantId}) zaten mevcut.");
        }

        // Editor rolünü ekle
        if (!await RoleExistsAsync(connectionString, "editor"))
        {
            var editorRole = new AppRole { Name = "editor" };
            var result = await CreateRoleAsync(connectionString, editorRole);
            if (result)
            {
                Console.WriteLine($"Editor rolü (Tenant ID: {tenantId}) başarıyla oluşturuldu.");
            }
            else
            {
                Console.WriteLine($"Editor rolü (Tenant ID: {tenantId}) oluşturulurken hata oluştu.");
            }
        }
        else
        {
            Console.WriteLine($"Editor rolü (Tenant ID: {tenantId}) zaten mevcut.");
        }
    }

    private async Task<bool> RoleExistsAsync(string connectionString, string roleName)
    {
        // Veritabanı bağlantısını kullanarak rol var mı kontrol et
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = "SELECT COUNT(*) FROM AspNetRoles WHERE Name = @RoleName";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@RoleName", roleName);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }
    }

    private async Task<bool> CreateRoleAsync(string connectionString, AppRole role)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();

            var query = "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@Name", role.Name);
            command.Parameters.AddWithValue("@NormalizedName", role.Name.ToUpper());
            command.Parameters.AddWithValue("@ConcurrencyStamp", Guid.NewGuid().ToString());

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
    }
    public async Task SeedAdminUserAsync(int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Kullanıcı var mı kontrolü
        var checkUserCmd = new MySqlCommand(
            "SELECT COUNT(*) FROM AspNetUsers WHERE UserName = 'admin@admin.com'",
            connection);
        var userExists = (long)await checkUserCmd.ExecuteScalarAsync() > 0;

        if (!userExists)
        {
            // Password hash oluştur
            var hasher = new PasswordHasher<AppUser>();
            var hashedPassword = hasher.HashPassword(null, "Admin123!");
            var userCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Kullanıcıyı ekle (tüm gerekli alanlarla)
            var insertUserCmd = new MySqlCommand(
                "INSERT INTO AspNetUsers " +
                "(Id, UserName, NormalizedUserName, Email, NormalizedEmail, " +
                "EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, " +
                "PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, " +
                "FullName, IsActive, UserCode) " +  // Ek alanlar burada
                "VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, " +
                "1, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, " +
                "0, 0, 1, 0, @FullName, @IsActive, @UserCode)", connection);

            // Temel alanlar
            insertUserCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@UserName", "admin@admin.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedUserName", "ADMIN@ADMIN.COM");
            insertUserCmd.Parameters.AddWithValue("@Email", "admin@admin.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedEmail", "ADMIN@ADMIN.COM");
            insertUserCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            insertUserCmd.Parameters.AddWithValue("@SecurityStamp", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@ConcurrencyStamp", Guid.NewGuid().ToString());

            // Ek alanlar
            insertUserCmd.Parameters.AddWithValue("@FullName", "Admin User");
            insertUserCmd.Parameters.AddWithValue("@IsActive", true);
            insertUserCmd.Parameters.AddWithValue("@UserCode", userCode);

            await insertUserCmd.ExecuteNonQueryAsync();

            // Admin rolünü ekle (yoksa)
            var checkRoleCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM AspNetRoles WHERE Name = 'admin'",
                connection);
            var roleExists = (long)await checkRoleCmd.ExecuteScalarAsync() > 0;

            if (!roleExists)
            {
                var insertRoleCmd = new MySqlCommand(
                    "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) " +
                    "VALUES (@Id, 'admin', 'ADMIN', @Stamp)", connection);

                insertRoleCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                insertRoleCmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
                await insertRoleCmd.ExecuteNonQueryAsync();
            }

            // Kullanıcıya admin rolünü ata
            var userRoleCmd = new MySqlCommand(
                "INSERT INTO AspNetUserRoles (UserId, RoleId) " +
                "SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r " +
                "WHERE u.UserName = 'admin@admin.com' AND r.Name = 'admin'", connection);

            await userRoleCmd.ExecuteNonQueryAsync();

            Console.WriteLine("Admin kullanıcısı başarıyla oluşturuldu.");
        }
        else
        {
            Console.WriteLine("Admin kullanıcısı zaten mevcut.");
        }
    }
    public async Task SeedEditorUserAsync(int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Kullanıcı var mı kontrolü
        var checkUserCmd = new MySqlCommand(
            "SELECT COUNT(*) FROM AspNetUsers WHERE UserName = 'editor@editor.com'",
            connection);
        var userExists = (long)await checkUserCmd.ExecuteScalarAsync() > 0;

        if (!userExists)
        {
            // Password hash oluştur
            var hasher = new PasswordHasher<AppUser>();
            var hashedPassword = hasher.HashPassword(null, "Password123!");
            var userCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            // Kullanıcıyı ekle (tüm gerekli alanlarla)
            var insertUserCmd = new MySqlCommand(
                "INSERT INTO AspNetUsers " +
                "(Id, UserName, NormalizedUserName, Email, NormalizedEmail, " +
                "EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, " +
                "PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, " +
                "FullName, IsActive, UserCode) " +
                "VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, " +
                "1, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, " +
                "0, 0, 1, 0, @FullName, @IsActive, @UserCode)", connection);

            // Temel alanlar
            insertUserCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@UserName", "editor@editor.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedUserName", "EDITOR@EDITOR.COM");
            insertUserCmd.Parameters.AddWithValue("@Email", "editor@editor.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedEmail", "EDITOR@EDITOR.COM");
            insertUserCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            insertUserCmd.Parameters.AddWithValue("@SecurityStamp", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@ConcurrencyStamp", Guid.NewGuid().ToString());

            // Ek alanlar
            insertUserCmd.Parameters.AddWithValue("@FullName", "Editor User");
            insertUserCmd.Parameters.AddWithValue("@IsActive", true);
            insertUserCmd.Parameters.AddWithValue("@UserCode", userCode);

            await insertUserCmd.ExecuteNonQueryAsync();

            // Editor rolünü ekle (yoksa)
            var checkRoleCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM AspNetRoles WHERE Name = 'editor'",
                connection);
            var roleExists = (long)await checkRoleCmd.ExecuteScalarAsync() > 0;

            if (!roleExists)
            {
                var insertRoleCmd = new MySqlCommand(
                    "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) " +
                    "VALUES (@Id, 'editor', 'EDITOR', @Stamp)", connection);

                insertRoleCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                insertRoleCmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
                await insertRoleCmd.ExecuteNonQueryAsync();
            }

            // Kullanıcıya editor rolünü ata
            var userRoleCmd = new MySqlCommand(
                "INSERT INTO AspNetUserRoles (UserId, RoleId) " +
                "SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r " +
                "WHERE u.UserName = 'editor@editor.com' AND r.Name = 'editor'", connection);

            await userRoleCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Editor kullanıcısı (Tenant ID: {tenantId}) başarıyla oluşturuldu.");
        }
        else
        {
            Console.WriteLine($"Editor kullanıcısı (Tenant ID: {tenantId}) zaten mevcut.");
        }
    }
    public async Task SeedCustomerUserAsync(int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // Kullanıcı var mı kontrolü
        var checkUserCmd = new MySqlCommand(
            "SELECT COUNT(*) FROM AspNetUsers WHERE UserName = 'customer@customer.com'",
            connection);
        var userExists = (long)await checkUserCmd.ExecuteScalarAsync() > 0;

        if (!userExists)
        {
            // Password hash oluştur
            var hasher = new PasswordHasher<AppUser>();
            var hashedPassword = hasher.HashPassword(null, "Password123!");
            var userCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            var insertUserCmd = new MySqlCommand(
                "INSERT INTO AspNetUsers " +
                "(Id, UserName, NormalizedUserName, Email, NormalizedEmail, " +
                "EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, " +
                "PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, " +
                "FullName, IsActive, UserCode) " +
                "VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, " +
                "1, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, " +
                "0, 0, 1, 0, @FullName, @IsActive, @UserCode)", connection);

            insertUserCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@UserName", "customer@customer.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedUserName", "CUSTOMER@CUSTOMER.COM");
            insertUserCmd.Parameters.AddWithValue("@Email", "customer@customer.com");
            insertUserCmd.Parameters.AddWithValue("@NormalizedEmail", "CUSTOMER@CUSTOMER.COM");
            insertUserCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
            insertUserCmd.Parameters.AddWithValue("@SecurityStamp", Guid.NewGuid().ToString());
            insertUserCmd.Parameters.AddWithValue("@ConcurrencyStamp", Guid.NewGuid().ToString());

            insertUserCmd.Parameters.AddWithValue("@FullName", "Customer User");
            insertUserCmd.Parameters.AddWithValue("@IsActive", true);
            insertUserCmd.Parameters.AddWithValue("@UserCode", userCode);

            await insertUserCmd.ExecuteNonQueryAsync();

            // Customer rolünü ekle (yoksa)
            var checkRoleCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM AspNetRoles WHERE Name = 'customer'",
                connection);
            var roleExists = (long)await checkRoleCmd.ExecuteScalarAsync() > 0;

            if (!roleExists)
            {
                var insertRoleCmd = new MySqlCommand(
                    "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) " +
                    "VALUES (@Id, 'customer', 'CUSTOMER', @Stamp)", connection);

                insertRoleCmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                insertRoleCmd.Parameters.AddWithValue("@Stamp", Guid.NewGuid().ToString());
                await insertRoleCmd.ExecuteNonQueryAsync();
            }

            // Kullanıcıya customer rolünü ata
            var userRoleCmd = new MySqlCommand(
                "INSERT INTO AspNetUserRoles (UserId, RoleId) " +
                "SELECT u.Id, r.Id FROM AspNetUsers u, AspNetRoles r " +
                "WHERE u.UserName = 'customer@customer.com' AND r.Name = 'customer'", connection);

            await userRoleCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"Customer kullanıcısı (Tenant ID: {tenantId}) başarıyla oluşturuldu.");
        }
        else
        {
            Console.WriteLine($"Customer kullanıcısı (Tenant ID: {tenantId}) zaten mevcut.");
        }
    }
    public async Task SeedJsonAsync(int tenantId)
    {
        try
        {
            // JSON içerik yapısını oluşturuyoruz
            var jsonContent = await CreateJsonContentAsync(tenantId);

            // JSON dosyasını kaydetmek
            var jsonFileName = await SaveJsonFileAsync(jsonContent, tenantId, _env);

            Console.WriteLine($"JSON dosyası başarıyla oluşturuldu: {jsonFileName}");
        }
        catch (Exception ex)
        {
            // Hata durumunda loglama veya başka bir işlem yapılabilir
            Console.Error.WriteLine($"JSON dosyası oluşturulurken bir hata oluştu: {ex.Message}");
        }
    }

    // JSON içeriğini oluştur
    private async Task<object> CreateJsonContentAsync(int tenantId)
    {
        // Burada tenant'a özel verileri oluşturmak için gerekli işlemleri yapabilirsiniz
        var jsonContent = new
        {
            Topbar = new
            {
                Links = new[]
                {
                    new { Text = "Anasayfa", Url = "/Shop/Home/Index" },
                    new { Text = "Ürünlerimiz", Url = "/Shop/Product/List" },
                    new { Text = "Kategoriler", Url = "/Shop/Category/Index" },
                    new { Text = "Para Puanlarım", Url = "/Shop/Coin/Index" },
                    new { Text = "Fırsatlar", Url = "/Shop/Coupon/Index" }

                }
            },
            Footer = new
            {
                Address = "Abide-i Hürriyet Cad. No:123",
                ButtonTitle = "İletişime Geç",
                ButtonUrl = "/Shop/Contact/Index",
                Mail = "info@modave.com",
                Phone = "0 555 555 55 55"
            },
            SiteMaps = new
            {
                Links = new[]
                {
                    new { Text = "Siparişleriniz", Url = "/Shop/Order/Index" },
                    new { Text = "Ürünlerimiz", Url = "/Shop/Product/List" },
                    new { Text = "Favorileriniz", Url = "/Shop/Favorite/Index" },
                    new { Text = "İletisim", Url = "/Shop/Contact/Index" },
                    new { Text = "Kariyer", Url = "/Shop/Career/Index" }

                }
            },
            Policies = new
            {
                Links = new[]
                {
                    new { Text = "Kargo Süreci", Url = "/Shop/Home/Index" },
                    new { Text = "İade ve Geri Ödeme", Url = "/Shop/Home/Index" },
                    new { Text = "Gizlilik Politikası", Url = "/Shop/Home/Index" },
                    new { Text = "Şartlar ve Koşullar", Url = "/Shop/Home/Index" },
                    new { Text = "SSS", Url = "/Shop/Home/Index" }

                }
            },
            MobileMenu = new
            {
                Links = new[]
                {
                    new { Text = "Anasayfa", Url = "/Shop/Home/Index" },
                    new { Text = "Ürünlerimiz", Url = "/Shop/Product/List" },
                    new { Text = "Kategoriler", Url = "/Shop/Category/Index" },
                    new { Text = "Para Puanlarım", Url = "/Shop/Coin/Index" },
                    new { Text = "Fırsatlar", Url = "/Shop/Coupon/Index" },
                    new { Text = "Profilim", Url = "/Shop/Profile/MyAccount" },

                },
                AddressTitle = "Mağaza Adresi",
                Address = "Abide-i Hürriyet Cad. No:123",
                ButtonTitle = "İletişime Geç",
                ButtonUrl = "/Shop/Contact/Index",
                Mail = "info@modave.com",
                Phone = "0 555 555 55 55"
            },
            InvoiceInfo = new
            {
                logoPath = "/uploads/764ed616-8f5f-4ef5-86b8-c798faf1eed2.png",
                storeName = "Modave Resmi Satıcısı",
                cityCountry = "Abide-i Hürriyet Cad. No:123",
                contactInfo = new
                {
                    tel = "0 212 555 55 55",
                    email = "info@modave.com"
                }
            }
        };

        return jsonContent;
    }

    // JSON dosyasını kaydet
    private async Task<string> SaveJsonFileAsync(object jsonContent, int tenantId, IWebHostEnvironment env)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        // Veritabanı bağlamı oluşturulur
        var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));

        // JSON verisini serialize ediyoruz
        var jsonContentString = JsonConvert.SerializeObject(jsonContent);

        // Config klasörünün yolunu alıyoruz
        var configDirectory = Path.Combine(env.WebRootPath, "config");

        // Eğer config klasörü yoksa, oluşturuyoruz
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }

        // JSON dosyasını config klasörüne kaydediyoruz
        var jsonFileName = $"store_{tenantId}.json";  // Tenant'a özel dosya adı
        var jsonFilePath = Path.Combine(configDirectory, jsonFileName);
        await File.WriteAllTextAsync(jsonFilePath, jsonContentString);

        // Veritabanına JSON dosyasının yolunu kaydetme işlemi
        await SaveJsonFilePathToDatabaseAsync(jsonFileName, tenantId);

        return jsonFileName;
    }

    // JSON dosyasının yolunu veritabanına kaydetmek için metod

    private async Task SaveJsonFilePathToDatabaseAsync(string jsonFileName, int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));

        using (var context = new EtradeContext(optionsBuilder.Options))
        {
            var storeSetting = await context.StoreSettings.FirstOrDefaultAsync();

            if (storeSetting != null)
            {
                storeSetting.JsonFilePath = $"/config/{jsonFileName}";
                await context.SaveChangesAsync();
            }
            else
            {
                // Eğer storeSetting yoksa (olması gerekir çünkü SeedStoreSettings'te oluşturduk)
                Console.WriteLine("Uyarı: StoreSetting bulunamadı, önce SeedStoreSettings çalıştırılmalı");
            }
        }
    }
    public async Task SeedSliderAsync(int tenantId)
    {
        // Tenant'a özel connection string alınır
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        // Veritabanı bağlamı oluşturulur
        var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40))); // Veritabanı versiyonunu belirtiyoruz

        // Context ile bağlantı oluşturulur
        using (var context = new EtradeContext(optionsBuilder.Options))
        {
            // Mevcut verilerin temizlenmesi (isteğe bağlı)
            // context.Sliders.RemoveRange(context.Sliders);
            // await context.SaveChangesAsync();

            // Slider verileri oluşturuluyor
            var sliders = new List<Slider>
        {
            new Slider
            {
                TopTitle = "Home Slider 1",
                Title = "Welcome to Our Store!",
                ButtonTitle = "Shop Now",
                ButtonUrl = "/shop",
                ImageUrl = "/uploads/slider2.png",
                IsActive = true,
                SliderCategory = "homeslider"
            },
            new Slider
            {
                TopTitle = "Home Slider 2",
                Title = "Big Sale Coming Soon!",
                ButtonTitle = "Learn More",
                ButtonUrl = "/sale",
                ImageUrl = "/uploads/slider2.png",
                IsActive = true,
                SliderCategory = "homeslider"
            },
            new Slider
            {
                TopTitle = "Main Slider",
                Title = "Introducing New Collection",
                ButtonTitle = "Explore Now",
                ButtonUrl = "/new-collection",
                ImageUrl = "/uploads/slider.png",
                IsActive = true,
                SliderCategory = "mainslider"
            }
        };

            // Slider verileri veritabanına ekleniyor
            await context.Sliders.AddRangeAsync(sliders);
            await context.SaveChangesAsync();
        }
    }
    public async Task SeedStoreSettings(int tenantId)
    {
        // Tenant'a özel connection string alınır
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        // Veritabanı bağlamı oluşturulur
        var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40))); // Veritabanı versiyonunu belirtiyoruz

        // Context ile bağlantı oluşturulur
        using (var context = new EtradeContext(optionsBuilder.Options))
        {
            // Mevcut verilerin temizlenmesi (isteğe bağlı)
            // context.Sliders.RemoveRange(context.Sliders);
            // await context.SaveChangesAsync();

            // Slider verileri oluşturuluyor
            var sliders = new List<StoreSetting>
        {
            new StoreSetting
            {
                LogoPath = "default_logo_path",
                LogoWhitePath = "default_logo_white_path",
                ShippingFee = 0.00m,
                IyzicoApiKey = "default_api_key",
                IyzicoSecretKey = "default_secret_key",
                IyzicoBaseUrl = "https://sandbox-api.iyzipay.com",
            }
        };

            // Slider verileri veritabanına ekleniyor
            await context.StoreSettings.AddRangeAsync(sliders);
            await context.SaveChangesAsync();
        }
    }
    public async Task SeedAllAddressDataAsync(int tenantId)
    {
        var connectionString = _tenantService.GetTenantConnectionString(tenantId);

        var sqlFiles = new List<string>
        {
            "il.sql",
            "ilce.sql",
            "semtler.sql",
            "mahalle.sql"
        };

        foreach (var fileName in sqlFiles)
        {
            await ExecuteSqlFileAsync(connectionString, fileName);
        }
    }

    private async Task ExecuteSqlFileAsync(string connectionString, string fileName)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "SeedSql", fileName);

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Dosya bulunamadı: {fileName}");
            return;
        }

        var sql = await File.ReadAllTextAsync(filePath);

        using (var connection = new MySqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var command = new MySqlCommand(sql, connection))
            {
                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"Yüklendi: {fileName}");
            }
        }
    }

    // Tüm verileri seed et
    public async Task SeedAllAsync(int tenantId)
    {
        await SeedRolesAsync(tenantId);
        await SeedStoreSettings(tenantId);
        await SeedSliderAsync(tenantId);
        await SeedAdminUserAsync(tenantId);
        await SeedAllAddressDataAsync(tenantId);
        await SeedJsonAsync(tenantId);
        await SeedEditorUserAsync(tenantId);
        await SeedCustomerUserAsync(tenantId);
    }
}