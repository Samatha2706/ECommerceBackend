using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECommerce.Tests.Services;

public class OrderServiceTests
{
    private static async Task<(ECommerceDbContext Context, SqliteConnection Connection)>
        CreateDbContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ECommerceDbContext(options);

        await context.Database.EnsureCreatedAsync();

        return (context, connection);
    }

    private static async Task<Product> CreateProductAsync(
        ECommerceDbContext context,
        decimal price = 100,
        int stock = 10)
    {
        var category = new Category
        {
            Name = $"Electronics-{Guid.NewGuid()}",
            Description = "Test category"
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Test Product-{Guid.NewGuid()}",
            Description = "Test product",
            Price = price,
            CategoryId = category.Id,
            IsActive = true,
            Inventory = new Inventory
            {
                Quantity = stock,
                ReorderLevel = 2
            }
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }

    private static async Task<User> CreateUserAsync(
    ECommerceDbContext context,
    int userId = 1)
    {
        var user = new User
        {
            Id = userId,
            FullName = "Test User",
            Email = $"test{userId}@example.com",
            PasswordHash = "TestPasswordHash",
            Role = UserRole.Customer
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private static async Task<Cart> CreateCartAsync(
        ECommerceDbContext context,
        int userId,
        Product product,
        int quantity)
    {
        var cart = new Cart
        {
            UserId = userId
        };

        cart.CartItems.Add(new CartItem
        {
            ProductId = product.Id,
            Product = product,
            Quantity = quantity
        });

        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        return cart;
    }

    [Fact]
    public async Task CheckoutAsync_WithValidCart_CreatesOrderAndReducesStock()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var product = await CreateProductAsync(
                context,
                price: 100,
                stock: 10);

            await CreateUserAsync(context, 1);

            await CreateCartAsync(
                context,
                userId: 1,
                product,
                quantity: 2);

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.CheckoutAsync(
                1,
                new CheckoutDto
                {
                    ShippingAddress = "Chennai, Tamil Nadu"
                });

            Assert.NotNull(result);
            Assert.Equal(200, result.TotalAmount);
            Assert.Equal("Chennai, Tamil Nadu", result.ShippingAddress);
            Assert.Equal(OrderStatus.Pending, result.Status);

            Assert.Single(result.Items);

            var orderItem = result.Items.First();

            Assert.Equal(product.Id, orderItem.ProductId);
            Assert.Equal(2, orderItem.Quantity);
            Assert.Equal(100, orderItem.UnitPrice);
            Assert.Equal(200, orderItem.Subtotal);

            var inventory = await context.Inventories
                .FirstAsync(i => i.ProductId == product.Id);

            Assert.Equal(8, inventory.Quantity);

            var cartItems = await context.CartItems.ToListAsync();

            Assert.Empty(cartItems);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartIsEmpty_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);

            context.Carts.Add(new Cart
            {
                UserId = 1
            });

            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CheckoutAsync(
                    1,
                    new CheckoutDto
                    {
                        ShippingAddress = "Chennai"
                    }));

            Assert.Equal(
                "Your cart is empty.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CheckoutAsync_WhenProductInactive_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var product = await CreateProductAsync(
                context,
                price: 100,
                stock: 10);
            await CreateUserAsync(context, 1);


            product.IsActive = false;
            await context.SaveChangesAsync();

            await CreateCartAsync(
                context,
                1,
                product,
                1);

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CheckoutAsync(
                    1,
                    new CheckoutDto
                    {
                        ShippingAddress = "Chennai"
                    }));

            Assert.Contains(
                "is no longer available",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CheckoutAsync_WhenStockIsInsufficient_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var product = await CreateProductAsync(
                context,
                price: 100,
                stock: 2);
            await CreateUserAsync(context, 1);


            await CreateCartAsync(
                context,
                1,
                product,
                5);

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CheckoutAsync(
                    1,
                    new CheckoutDto
                    {
                        ShippingAddress = "Chennai"
                    }));

            Assert.Contains(
                "Insufficient stock",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task CheckoutAsync_WhenAddressIsEmpty_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.CheckoutAsync(
                    1,
                    new CheckoutDto
                    {
                        ShippingAddress = "   "
                    }));

            Assert.Equal(
                "Shipping address is required.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderBelongsToUser_ReturnsOrder()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var product = await CreateProductAsync(context);
            await CreateUserAsync(context, 1);

            var order = new Order
            {
                UserId = 1,
                TotalAmount = 100,
                ShippingAddress = "Chennai",
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };

            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = 1,
                UnitPrice = product.Price
            });

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetByIdAsync(
                1,
                order.Id);

            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
            Assert.Equal(1, result.UserId);
            Assert.Single(result.Items);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderBelongsToAnotherUser_ReturnsNull()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);

            var order = new Order
            {
                UserId = 1,
                TotalAmount = 100,
                ShippingAddress = "Chennai",
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetByIdAsync(
                999,
                order.Id);

            Assert.Null(result);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetMyOrdersAsync_ReturnsOnlyCurrentUsersOrders()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);
            await CreateUserAsync(context, 2);
            context.Orders.AddRange(
                new Order
                {
                    UserId = 1,
                    TotalAmount = 100,
                    ShippingAddress = "Chennai",
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow.AddMinutes(-10)
                },
                new Order
                {
                    UserId = 1,
                    TotalAmount = 200,
                    ShippingAddress = "Bangalore",
                    Status = OrderStatus.Paid,
                    OrderDate = DateTime.UtcNow
                },
                new Order
                {
                    UserId = 2,
                    TotalAmount = 300,
                    ShippingAddress = "Mumbai",
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetMyOrdersAsync(1);

            Assert.Equal(2, result.Count);
            Assert.All(
                result,
                order => Assert.Equal(1, order.UserId));
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAllOrdersAsync_ReturnsAllOrders()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);
            await CreateUserAsync(context, 2);
            context.Orders.AddRange(
                new Order
                {
                    UserId = 1,
                    TotalAmount = 100,
                    ShippingAddress = "Chennai",
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow
                },
                new Order
                {
                    UserId = 2,
                    TotalAmount = 200,
                    ShippingAddress = "Bangalore",
                    Status = OrderStatus.Paid,
                    OrderDate = DateTime.UtcNow
                });

            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetAllOrdersAsync();

            Assert.Equal(2, result.Count);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByIdForAdminAsync_WhenOrderExists_ReturnsOrder()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);
            var order = new Order
            {
                UserId = 1,
                TotalAmount = 150,
                ShippingAddress = "Chennai",
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetByIdForAdminAsync(order.Id);

            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
            Assert.Equal(150, result.TotalAmount);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByIdForAdminAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.GetByIdForAdminAsync(999);

            Assert.Null(result);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderExists_UpdatesStatusAndSendsNotification()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);

            var order = new Order
            {
                UserId = 1,
                TotalAmount = 100,
                ShippingAddress = "Chennai",
                Status = OrderStatus.Pending,
                OrderDate = DateTime.UtcNow
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.UpdateStatusAsync(
                order.Id,
                new UpdateOrderStatusDto
                {
                    Status = OrderStatus.Shipped
                });

            Assert.NotNull(result);
            Assert.Equal(
                OrderStatus.Shipped,
                result.Status);

            var updatedOrder = await context.Orders
                .FirstAsync(o => o.Id == order.Id);

            Assert.Equal(
                OrderStatus.Shipped,
                updatedOrder.Status);

            notificationMock.Verify(
                service => service.NotifyOrderStatusChangedAsync(
                    1,
                    order.Id,
                    "Shipped"),
                Times.Once);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            var notificationMock =
                new Mock<IOrderNotificationService>();

            var service = new OrderService(
                context,
                notificationMock.Object);

            var result = await service.UpdateStatusAsync(
                999,
                new UpdateOrderStatusDto
                {
                    Status = OrderStatus.Shipped
                });

            Assert.Null(result);

            notificationMock.Verify(
                service => service.NotifyOrderStatusChangedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>()),
                Times.Never);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}