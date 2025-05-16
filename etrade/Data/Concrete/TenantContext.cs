using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using etrade.Entity;
using etrade.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace etrade.Data.Concrete
{
    public class TenantContext : IdentityDbContext<TenantUser, TenantRole, string>
    {
        public TenantContext(DbContextOptions<TenantContext> options) : base(options)
        {
        }

        public DbSet<TenantStore> TenantStores { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<LicensePayment> LicensePayments { get; set; }
        public DbSet<TenantCustomer> TenantCustomers { get; set; }
        public DbSet<City> Iller { get; set; }
        public DbSet<CancellationRequest> CancellationRequests { get; set; }
        public DbSet<FreezePayment> FreezePayments { get; set; }
        public DbSet<Message> Messages { get; set; }


        public DbSet<DemoRequest> DemoRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<License>()
    .HasOne(l => l.TenantCustomer)
    .WithMany(c => c.Licenses)
    .HasForeignKey(l => l.CustomerId);

            modelBuilder.Entity<LicensePayment>()
                .HasOne(p => p.License)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.LicenseId);

            modelBuilder.Entity<TenantCustomer>()
                .HasOne(c => c.City)
                .WithMany(i => i.TenantCustomers)
                .HasForeignKey(c => c.IlId);

            modelBuilder.Entity<TenantStore>()
                .HasOne(ls => ls.License)
                .WithMany(l => l.TenantStores)
                .HasForeignKey(ls => ls.LicenseId)
                .OnDelete(DeleteBehavior.SetNull); // Lisans silinirse mağaza kalabilir
                                                   // Gerekirse seed data veya mapping burada yapılabilir
            modelBuilder.Entity<CancellationRequest>()
                .HasOne(x => x.License)
                .WithMany(x => x.CancellationRequests)
                .HasForeignKey(x => x.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FreezePayment>()
                .HasOne(x => x.License)
                .WithMany(x => x.FreezePayments)
                .HasForeignKey(x => x.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TenantCustomer>()
                .HasOne(tc => tc.User)
                .WithOne(u => u.TenantCustomer)
                .HasForeignKey<TenantCustomer>(tc => tc.UserId);
        }
    }
}