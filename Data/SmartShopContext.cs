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
            // Ensure the table names match the database created in XAMPP/MySQL
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<Review>().ToTable("Review");
        }
    }
}