using ECommerce.ProductService.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(category => category.Name)
                .IsUnique();

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(p => p.SKU)
                .IsUnique();

            entity.Property(product => product.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(product => product.Inventory)
                .WithOne(inventory => inventory.Product)
                .HasForeignKey<Inventory>(inventory => inventory.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
}