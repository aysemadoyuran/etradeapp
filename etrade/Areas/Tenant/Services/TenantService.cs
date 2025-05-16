using System;
using System.Linq;
using System.Threading.Tasks;
using etrade.Data.Concrete;
using etrade.Entity;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using static etrade.Areas.Tenant.Controllers.StoreController;

namespace etrade.Areas.Tenant.Services
{
    public class TenantService
    {
        private readonly EtradeContext _etradeContext;  // Mağaza veritabanı için
        private readonly TenantContext _tenantDbContext;  // Tenant bilgileri için

        public TenantService(EtradeContext etradeContext, TenantContext tenantDbContext)
        {
            _etradeContext = etradeContext;
            _tenantDbContext = tenantDbContext;
        }

        public string GetTenantConnectionString(int tenantId)
        {
            var tenant = _tenantDbContext.TenantStores.FirstOrDefault(t => t.Id == tenantId);
            var tenantConnStr = EncryptionHelper.Decrypt(tenant.ConnectionString);


            if (tenant == null)
                throw new Exception("Tenant bulunamadı.");

            return tenantConnStr;
        }
        public List<(int TenantId, string Name, string ConnectionString)> GetAllTenantConnectionStrings()
        {
            var tenants = _tenantDbContext.TenantStores.ToList();

            var tenantConnStrList = new List<(int, string, string)>();

            foreach (var tenant in tenants)
            {
                var decryptedConnStr = EncryptionHelper.Decrypt(tenant.ConnectionString);
                tenantConnStrList.Add((tenant.Id, tenant.StoreName, decryptedConnStr));
            }

            return tenantConnStrList;
        }

        public async Task CreateTenantDatabaseAsync(int tenantId)
        {
            var tenant = _tenantDbContext.TenantStores.FirstOrDefault(t => t.Id == tenantId);

            if (tenant == null)
                throw new Exception("Tenant bulunamadı.");

            var tenantConnStr = EncryptionHelper.Decrypt(tenant.ConnectionString);
            var builder = new MySqlConnectionStringBuilder(tenantConnStr);
            var databaseName = builder.Database;

            builder.Database = ""; // Sisteme bağlanmak için boş DB
            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();

                var cmd = connection.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;";
                await cmd.ExecuteNonQueryAsync();
            }

            // Yeni oluşturulan veritabanına EF ile bağlan
            var optionsBuilder = new DbContextOptionsBuilder<EtradeContext>();
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 40)); // Burayı MySQL sürümüne göre ayarla
            optionsBuilder.UseMySql(tenantConnStr, serverVersion);
            using (var etradeContext = new EtradeContext(optionsBuilder.Options))
            {
                await etradeContext.Database.MigrateAsync();
            }
        }
        public TenantStore GetTenantByDomain(string domain)
        {
            // Domain'i TenantContext'ten alıp tenant'ı buluyoruz
            return _tenantDbContext.TenantStores
                .AsNoTracking()
                .FirstOrDefault(t => t.Domain == domain); // Burada domain'e göre tenant sorguluyoruz
        }
        public List<string> GetAllTenantDomains()
        {
            return _tenantDbContext.TenantStores
                           .AsNoTracking()
                           .Select(t => t.Domain)  // Domainleri seçiyoruz
                           .ToList();
        }
    }
}