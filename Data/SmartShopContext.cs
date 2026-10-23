using Microsoft.EntityFrameworkCore;
using SmartShopAPI.Models;

namespace SmartShopAPI.Data
{
    public class SmartShopContext : DbContext
    {
        public SmartShopContext(DbContextOptions<SmartShopContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Đảm bảo tên bảng khớp với CSDL bạn tạo trong XAMPP/MySQL
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Review>().ToTable("Review");
        }
    }
}