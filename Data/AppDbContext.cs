using ABCapoAPi.Data; // تأكد من وجود هذا السطر لاستيراد Color, Size, Copy
using Microsoft.EntityFrameworkCore;
// إذا كانت المنتجات موجودة في مجلد Models، احتفظ بـ using ABCapoAPi.Models;

namespace ABCapoAPi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<OrderStatus> OrderStatuses { get; set; }
    public DbSet<Country> Countries { get; set; }

    // --- إضافة الجداول الجديدة (DbSets) ---
    public DbSet<Color> Colors { get; set; }
    public DbSet<Size> Sizes { get; set; }
    public DbSet<CopyOption> Copies { get; set; }

    // ------------------------------------

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. ضبط دقة أسعار المنتجات
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        // 2. ضبط دقة المجموع الكامل للطلب
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        // 3. ضبط دقة سعر المنتج داخل تفاصيل الطلب
        modelBuilder.Entity<OrderItem>()
            .Property(o => o.Price)
            .HasPrecision(18, 2);

        // 4. ضبط دقة مبلغ الدفع
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);


        // --- إعدادات العلاقات الموجودة ---
        modelBuilder.Entity<Order>()
            .HasOne(o => o.OrderStatus)
            .WithMany()
            .HasForeignKey(o => o.StatusID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Country)
            .WithMany()
            .HasForeignKey(u => u.CountryID)
            .OnDelete(DeleteBehavior.Restrict);

        // --- إعدادات العلاقات الجديدة (Product -> Color/Size/Copy) ---
        // هذه الإعدادات تسمح لك بكتابة product.Color.Name في الكود
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Color)
            .WithMany()
            .HasForeignKey(p => p.ColorID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Size)
            .WithMany()
            .HasForeignKey(p => p.SizeID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Copy)
            .WithMany()
            .HasForeignKey(p => p.CopyID)
            .OnDelete(DeleteBehavior.Restrict);
        // -------------------------------------------------------------

        base.OnModelCreating(modelBuilder);
    }
}