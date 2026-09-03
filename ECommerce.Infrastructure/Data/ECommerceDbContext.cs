using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.Infrastructure.Data;

public class ECommerceDbContext : DbContext, IApplicationDbContext
{
    public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(
    CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(150).IsRequired();
            entity.Property(user => user.FullName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(category => category.Name).IsUnique();
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name).HasMaxLength(150).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(product => product.Inventory)
                .WithOne(inventory => inventory.Product)
                .HasForeignKey<Inventory>(inventory => inventory.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.ShippingAddress).HasMaxLength(250).IsRequired();

            entity.HasOne(order => order.User)
                .WithMany(user => user.Orders)
                .HasForeignKey(order => order.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(item => item.Order)
                .WithMany(order => order.OrderItems)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.Amount).HasPrecision(18, 2);

            entity.HasOne(payment => payment.Order)
                .WithOne(order => order.Payment)
                .HasForeignKey<Payment>(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasOne(cart => cart.User)
                .WithOne()
                .HasForeignKey<Cart>(cart => cart.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cart => cart.UserId)
                .IsUnique();
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasOne(item => item.Cart)
                .WithMany(cart => cart.CartItems)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Product)
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => new
            {
                item.CartId,
                item.ProductId
            })
            .IsUnique();
        });

        var adminPasswordHash =
   "$2a$11$dHvhpXMc1FlBXhifuzTWWu9e9eF3uHqbmkDrkW1h4HCDinIK/L4CO";

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 17,
                FullName = "System Administrator",
                Email = "somidiAdmin@gmail.com",
                PasswordHash = adminPasswordHash,
                Role = UserRole.Admin,
                CreatedAt = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
            });

        var categories = new[]
{
    new Category
    {
        Id = 3,
        Name = "Electronics",
        Description = "Electronic devices and accessories"
    },
    new Category
    {
        Id = 4,
        Name = "Clothing",
        Description = "Clothing and fashion products"
    },
    new Category
    {
        Id = 5,
        Name = "Books",
        Description = "Books and learning materials"
    },
    new Category
    {
        Id = 6,
        Name = "Home & Kitchen",
        Description = "Products for home and kitchen"
    },
    new Category
    {
        Id = 7,
        Name = "Sports",
        Description = "Sports and fitness products"
    }
};

        modelBuilder.Entity<Category>().HasData(categories);

        var products = new[]
        {
    new Product
    {
        Id = 3,
        Name = "Wireless Headphones",
        Description = "Bluetooth over-ear wireless headphones",
        Price = 2499.00m,
        IsActive = true,
        CategoryId = 3
    },
    new Product
    {
        Id = 4,
        Name = "Smart Watch",
        Description = "Fitness tracking smart watch",
        Price = 3999.00m,
        IsActive = true,
        CategoryId = 3
    },
    new Product
    {
        Id = 5,
        Name = "Mechanical Keyboard",
        Description = "RGB mechanical keyboard for computers",
        Price = 2999.00m,
        IsActive = true,
        CategoryId = 3
    },
    new Product
    {
        Id = 6,
        Name = "Wireless Mouse",
        Description = "Ergonomic wireless mouse",
        Price = 1299.00m,
        IsActive = true,
        CategoryId = 3
    },

    new Product
    {
        Id = 7,
        Name = "Cotton T-Shirt",
        Description = "Comfortable regular-fit cotton T-shirt",
        Price = 799.00m,
        IsActive = true,
        CategoryId = 4
    },
    new Product
    {
        Id = 8,
        Name = "Slim Fit Jeans",
        Description = "Classic slim-fit denim jeans",
        Price = 1799.00m,
        IsActive = true,
        CategoryId = 4
    },
    new Product
    {
        Id = 9,
        Name = "Running Shoes",
        Description = "Lightweight running shoes",
        Price = 2499.00m,
        IsActive = true,
        CategoryId = 4
    },

    new Product
    {
        Id = 10,
        Name = "Clean Code",
        Description = "A practical guide to writing clean software",
        Price = 699.00m,
        IsActive = true,
        CategoryId = 5
    },
    new Product
    {
        Id = 11,
        Name = "C# Programming Guide",
        Description = "Programming fundamentals and C# concepts",
        Price = 899.00m,
        IsActive = true,
        CategoryId = 5
    },
    new Product
    {
        Id = 12,
        Name = "ASP.NET Core Development",
        Description = "Guide to building modern web applications",
        Price = 1099.00m,
        IsActive = true,
        CategoryId = 5
    },

    new Product
    {
        Id = 13,
        Name = "Coffee Maker",
        Description = "Automatic coffee maker for home",
        Price = 3499.00m,
        IsActive = true,
        CategoryId = 6
    },
    new Product
    {
        Id = 14,
        Name = "Stainless Steel Water Bottle",
        Description = "Insulated stainless steel water bottle",
        Price = 899.00m,
        IsActive = true,
        CategoryId = 6
    },
    new Product
    {
        Id = 15,
        Name = "Kitchen Storage Set",
        Description = "Reusable containers for kitchen storage",
        Price = 1299.00m,
        IsActive = true,
        CategoryId = 6
    },

    new Product
    {
        Id = 16,
        Name = "Yoga Mat",
        Description = "Non-slip exercise and yoga mat",
        Price = 999.00m,
        IsActive = true,
        CategoryId = 7
    },
    new Product
    {
        Id = 17,
        Name = "Gym Backpack",
        Description = "Durable backpack for gym and sports",
        Price = 1599.00m,
        IsActive = true,
        CategoryId = 7
    }
};

        modelBuilder.Entity<Product>().HasData(products);

        var inventories = new[]
        {
    new Inventory
    {
        Id = 3,
        ProductId = 3,
        Quantity = 25,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 4,
        ProductId = 4,
        Quantity = 15,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 5,
        ProductId = 5,
        Quantity = 3,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 6,
        ProductId = 6,
        Quantity = 20,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 7,
        ProductId = 7,
        Quantity = 30,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 8,
        ProductId = 8,
        Quantity = 18,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 9,
        ProductId = 9,
        Quantity = 12,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 10,
        ProductId = 10,
        Quantity = 10,
        ReorderLevel = 3
    },
    new Inventory
    {
        Id = 11,
        ProductId = 11,
        Quantity = 8,
        ReorderLevel = 3
    },
    new Inventory
    {
        Id = 12,
        ProductId = 12,
        Quantity = 6,
        ReorderLevel = 3
    },
    new Inventory
    {
        Id = 13,
        ProductId = 13,
        Quantity = 7,
        ReorderLevel = 3
    },
    new Inventory
    {
        Id = 14,
        ProductId = 14,
        Quantity = 20,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 15,
        ProductId = 15,
        Quantity = 9,
        ReorderLevel = 3
    },
    new Inventory
    {
        Id = 16,
        ProductId = 16,
        Quantity = 1,
        ReorderLevel = 5
    },
    new Inventory
    {
        Id = 17,
        ProductId = 17,
        Quantity = 14,
        ReorderLevel = 5
    }
};

        modelBuilder.Entity<Inventory>().HasData(inventories);

    }
}