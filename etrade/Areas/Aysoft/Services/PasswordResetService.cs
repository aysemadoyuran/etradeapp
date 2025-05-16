using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Aysoft.Services
{
    public class PasswordResetService
    {
        private readonly TenantContext _context;

        public PasswordResetService(TenantContext context)
        {
            _context = context;
        }

        public async Task<bool> ResetUserPasswordAsync(int tenantCustomerId, string email, string newPassword)
        {
            // 1. License tablosuna TenantCustomerId ile ulaşıyoruz
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.CustomerId == tenantCustomerId);

            if (license == null) return false;

            // 2. LicenseId ile TenantStores tablosuna geçiyoruz
            var tenantStore = await _context.TenantStores
                .FirstOrDefaultAsync(t => t.LicenseId == license.Id);

            if (tenantStore == null) return false;

            // 3. Şifreli connection string’i çözüyoruz
            var decryptedConnectionString = EncryptionHelper.Decrypt(tenantStore.ConnectionString);

            // 4. Yeni DbContext options oluşturuyoruz
            var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();

            // MySQL ServerVersion otomatik olarak algılanır
            var serverVersion = ServerVersion.AutoDetect(decryptedConnectionString);

            optionsBuilder.UseMySql(decryptedConnectionString, serverVersion);

            using var tenantDbContext = new EtradeContext(optionsBuilder.Options);

            // 5. Kullanıcıyı bul ve şifresini güncelle
            var user = await tenantDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            var passwordHasher = new PasswordHasher<AppUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

            await tenantDbContext.SaveChangesAsync();
            return true;
        }
    }
}