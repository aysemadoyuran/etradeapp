using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using etrade.Entity;
using etrade.Models;
// AppUser ve AppRole sınıflarını burada eklememiz gerekebilir

namespace etrade.Data.Concrete
{
    public class EtradeContext : IdentityDbContext<AppUser, AppRole, string>  // IdentityDbContext'i kullanıyoruz
    {
        public EtradeContext(DbContextOptions<EtradeContext> options)
            : base(options)
        {
        }

        // DbSet Tanımlamaları
        public DbSet<Product> Products { get; set; }
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantImage> ProductVariantImages { get; set; }
        public DbSet<ColorImage> ColorImages { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }

        // Identity tabloları burada otomatik olarak ekleniyor.
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<AppRole> AppRoles { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }  // Yeni eklendi
        public DbSet<Il> Iller { get; set; } = null!;
        public DbSet<Ilce> Ilceler { get; set; } = null!;
        public DbSet<District> Districts { get; set; }
        public DbSet<Street> Streets { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<ItemsBasket> ItemsBaskets { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<DiscountProduct> DiscountProducts { get; set; }
        public DbSet<DiscountCategory> DiscountCategories { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RefundRequest> RefundRequests { get; set; }
        public DbSet<RefundedItem> RefundedItems { get; set; }
        public DbSet<UserCoin> UserCoins { get; set; }
        public DbSet<StoreSetting> StoreSettings { get; set; }



        // OnModelCreating - Veritabanı ilişkilerini yapılandırmak için
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ürün kodunun benzersiz olmasını sağlıyoruz
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique();
            modelBuilder.Entity<Order>()
.HasIndex(o => o.OrderCode)
.IsUnique();
            modelBuilder.Entity<RefundRequest>()
                .HasIndex(o => o.RefundCode)
                .IsUnique();

            // Product ve Category arasındaki ilişki
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // Product ve SubCategory arasındaki ilişki
            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // Product ve ProductVariant arasındaki ilişki
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.ProductVariants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // ProductVariant ve Size arasındaki ilişki
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Size)
                .WithMany(s => s.ProductVariants)
                .HasForeignKey(pv => pv.SizeId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // ProductVariant ve Color arasındaki ilişki
            modelBuilder.Entity<ProductVariant>()
                .HasOne(pv => pv.Color)
                .WithMany(c => c.ProductVariants)
                .HasForeignKey(pv => pv.ColorId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // ProductVariantImage ve ProductVariant arasındaki ilişki
            modelBuilder.Entity<ProductVariantImage>()
                .HasOne(pvi => pvi.ProductVariant)
                .WithMany(pv => pv.ProductVariantImages)
                .HasForeignKey(pvi => pvi.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            // ColorImage ve Color arasındaki ilişki
            modelBuilder.Entity<ColorImage>()
                .HasOne(ci => ci.Color)
                .WithMany(c => c.ColorImages)
                .HasForeignKey(ci => ci.ColorId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete
            modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.ProductVariant)
            .WithMany(pv => pv.StockMovements)
            .HasForeignKey(sm => sm.ProductVariantId);
            // İl - İlçe (1-N)
            modelBuilder.Entity<Il>()
                .HasMany(il => il.Ilceler)
                .WithOne(ilce => ilce.Il)
                .HasForeignKey(ilce => ilce.IlId)
                .OnDelete(DeleteBehavior.Cascade);

            // İlçe - Semt (1-N)
            modelBuilder.Entity<Ilce>()
                .HasMany(ilce => ilce.Districts)
                .WithOne(district => district.Ilce)
                .HasForeignKey(district => district.IlceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Semt - Mahalle (1-N) (GÜNCELLENDİ!)
            modelBuilder.Entity<District>()
                .HasMany(d => d.Streets)
                .WithOne(s => s.District)
                .HasForeignKey(s => s.SemtId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Address>()
       .HasOne(a => a.User)
       .WithMany(u => u.Addresses)
       .HasForeignKey(a => a.UserId)
       .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Il)
                .WithMany()
                .HasForeignKey(a => a.IlId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Ilce)
                .WithMany()
                .HasForeignKey(a => a.IlceId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.District)
                .WithMany()
                .HasForeignKey(a => a.SemtId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Street)
                .WithMany()
                .HasForeignKey(a => a.MahalleId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Address>()
                .Property(a => a.IlId)
                .HasColumnType("TINYINT");
            modelBuilder.Entity<ItemsBasket>()
                .HasOne(bi => bi.Basket)
                .WithMany(b => b.ItemsBaskets)
                .HasForeignKey(bi => bi.BasketId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProductVariant ile ilişki
            modelBuilder.Entity<ItemsBasket>()
                .HasOne(bi => bi.ProductVariant)  // ProductVariant üzerinden ilişkileri alıyoruz
                .WithMany()
                .HasForeignKey(bi => bi.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
       .HasMany(o => o.OrderItems)
       .WithOne(oi => oi.Order)
       .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.ProductVariant)
                .WithMany()
                .HasForeignKey(oi => oi.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict); // İsteğe bağlı: Ürün varyantı silindiğinde OrderItem silinmesin
            modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Order)
    .WithMany(o => o.OrderItems)
    .HasForeignKey(oi => oi.OrderId)
    .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<PaymentMethod>()
                .HasOne(p => p.Order)
                .WithOne(o => o.PaymentMethod)
                .HasForeignKey<PaymentMethod>(p => p.OrderId);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId);
            modelBuilder.Entity<Favorite>()
.HasKey(f => new { f.UserId, f.ProductId }); // Bir kullanıcı, aynı ürünü birden fazla kez favorilerine ekleyemez

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites) // Kullanıcıya ait birden fazla favori olabilir
                .HasForeignKey(f => f.UserId);

            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Product)
                .WithMany(p => p.Favorites) // Bir ürün birden fazla kullanıcı tarafından favori yapılabilir
                .HasForeignKey(f => f.ProductId);
            // Comment ve Product arasındaki ilişki
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Product)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Yorum silindiğinde ürüne bağlı yorumların da silinmesi

            // Comment ve User arasındaki ilişki
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Kullanıcı silindiğinde yorumlarının da silinmesi

            // Discount-DiscountProduct ilişkisi (Many-to-Many)
            modelBuilder.Entity<DiscountProduct>()
                .HasKey(dp => new { dp.DiscountId, dp.ProductId });

            modelBuilder.Entity<DiscountProduct>()
                .HasOne(dp => dp.Discount)
                .WithMany(d => d.DiscountProducts)
                .HasForeignKey(dp => dp.DiscountId);

            modelBuilder.Entity<DiscountProduct>()
                .HasOne(dp => dp.Product)
                .WithMany(p => p.DiscountProducts)
                .HasForeignKey(dp => dp.ProductId);

            // İndirim ve Kategori ilişkisi (Many-to-Many)
            modelBuilder.Entity<DiscountCategory>()
                .HasKey(dc => new { dc.DiscountId, dc.CategoryId });

            modelBuilder.Entity<DiscountCategory>()
                .HasOne(dc => dc.Discount)
                .WithMany(d => d.DiscountCategories)
                .HasForeignKey(dc => dc.DiscountId);

            modelBuilder.Entity<DiscountCategory>()
                .HasOne(dc => dc.Category)
                .WithMany(c => c.DiscountCategories)
                .HasForeignKey(dc => dc.CategoryId);

            modelBuilder.Entity<Product>()
         .HasOne(p => p.Discount) // Her Product'ın bir Discount'u olabilir
         .WithMany(d => d.Products) // Bir Discount birden fazla Product'a sahip olabilir
         .HasForeignKey(p => p.DiscountId) // Product'daki DiscountId ile ilişkilendir
         .OnDelete(DeleteBehavior.SetNull); // Discount silinirse, Product.DiscountId null olur
                                            // OrderItem ve Discount arasındaki ilişkiyi tanımlıyoruz
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Discount)  // OrderItem'ın bir Discount'u olabilir
                .WithMany()  // Discount, birçok OrderItem'a sahip olabilir
                .HasForeignKey(oi => oi.DiscountId)  // OrderItem'daki DiscountId foreign key
                .OnDelete(DeleteBehavior.SetNull);  // İndirim silindiğinde, OrderItem'daki DiscountId null olur

            // Coupon tablosuyla ilgili ilişkiler ve yapılandırmalar


            // Coupon ile CouponUsage arasındaki ilişkiyi tanımlama
            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.Coupon)
                .WithMany(c => c.CouponUsages)
                .HasForeignKey(cu => cu.CouponId)
                .OnDelete(DeleteBehavior.Cascade);  // Bir kupon silindiğinde ona ait kullanımlar da silinsin

            // Kullanıcı ve CouponUsage ilişkisi
            modelBuilder.Entity<CouponUsage>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.CouponUsages)
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
           .HasOne(n => n.User)
           .WithMany(u => u.Notifications)
           .HasForeignKey(n => n.UserId)
           .OnDelete(DeleteBehavior.SetNull);
            // RefundRequest ve RefundedItem arasında ilişki
            modelBuilder.Entity<RefundedItem>()
                .HasOne(ri => ri.RefundRequest)
                .WithMany(rr => rr.RefundedItems)
                .HasForeignKey(ri => ri.RefundRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // RefundRequest ve PaymentMethod arasında ilişki
            // RefundRequest ve PaymentMethod arasında ilişkiyi PaymentMethodId ile kurun
            modelBuilder.Entity<RefundRequest>()
                .HasOne(rr => rr.PaymentMethod)
                .WithMany()  // PaymentMethod'da RefundRequests koleksiyonu olmasın
                .HasForeignKey(rr => rr.PaymentMethodId)  // PaymentMethodId üzerinden ilişki kuruyoruz
                .OnDelete(DeleteBehavior.Restrict);

            // RefundRequest ve Order arasında ilişki
            modelBuilder.Entity<RefundRequest>()
                .HasOne(rr => rr.Order)
                .WithMany(o => o.RefundRequests)
                .HasForeignKey(rr => rr.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // RefundedItem ve ProductVariant arasında ilişki
            modelBuilder.Entity<RefundedItem>()
                .HasOne(ri => ri.ProductVariant)
                .WithMany(pv => pv.RefundedItems)
                .HasForeignKey(ri => ri.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
            //Coin-User İlişkisi
            modelBuilder.Entity<UserCoin>()
    .HasOne(uc => uc.User)
    .WithOne(u => u.UserCoin)
    .HasForeignKey<UserCoin>(uc => uc.UserId);

        }
    }






}





