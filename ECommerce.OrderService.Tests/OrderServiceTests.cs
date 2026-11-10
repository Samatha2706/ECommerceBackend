
using ECommerce.OrderService.Data;
using ECommerce.OrderService.DTOs.Carts;
using ECommerce.OrderService.DTOs.Orders;
using ECommerce.OrderService.Services;
using Microsoft.EntityFrameworkCore;
using OrderServiceClass = ECommerce.OrderService.Services.OrderService;

namespace ECommerce.OrderService.Tests;

public class OrderServiceTests
{
    private sealed class FakeProductServiceClient : IProductServiceClient
    {
        public ProductResponse? Product { get; set; }

        public Task<ProductResponse?> GetProductByIdAsync(int productId)
        {
            return Task.FromResult(Product);
        }
    }

    private sealed class FakePaymentServiceClient : IPaymentServiceClient
    {
        public PaymentResponse? Payment { get; set; }

        public Task<PaymentResponse?> ProcessPaymentAsync(
            int orderId,
            decimal amount,
            string paymentMethod)
        {
            return Task.FromResult(Payment);
        }
    }

    [Fact]
    public async Task GetCartAsync_WhenCartDoesNotExist_ReturnsEmptyCart()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var productClient = new FakeProductServiceClient();
        var paymentClient = new FakePaymentServiceClient();

        var service = new OrderServiceClass(
            context, productClient, paymentClient);

        var result = await service.GetCartAsync(101);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalAmount);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenQuantityIsZero_ThrowsArgumentException()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var service = new OrderServiceClass(
            context,
            new FakeProductServiceClient(),
            new FakePaymentServiceClient());

        var dto = new AddCartItemDto
        {
            ProductId = 1,
            Quantity = 0
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddCartItemAsync(101, dto));

        Assert.Equal(
            "Quantity must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenProductIsUnavailable_ThrowsInvalidOperationException()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var productClient = new FakeProductServiceClient
        {
            Product = null
        };

        var service = new OrderServiceClass(
            context,
            productClient,
            new FakePaymentServiceClient());

        var dto = new AddCartItemDto
        {
            ProductId = 1,
            Quantity = 2
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddCartItemAsync(101, dto));
    }


    [Fact]
    public async Task AddCartItemAsync_WhenProductIsValid_AddsItemAndCalculatesSubtotal()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var productClient = new FakeProductServiceClient
        {
            Product = new ProductResponse
            {
                Id = 1,
                Name = "Laptop",
                Price = 50000m,
                IsActive = true
            }
        };

        var service = new OrderServiceClass(
            context,
            productClient,
            new FakePaymentServiceClient());

        var dto = new AddCartItemDto
        {
            ProductId = 1,
            Quantity = 2
        };

        var result = await service.AddCartItemAsync(101, dto);

        var item = Assert.Single(result.Items);
        Assert.Equal("Laptop", item.ProductName);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(50000m, item.UnitPrice);
        Assert.Equal(100000m, item.Subtotal);
        Assert.Equal(100000m, result.TotalAmount);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenSameProductIsAddedAgain_IncreasesExistingQuantity()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var productClient = new FakeProductServiceClient
        {
            Product = new ProductResponse
            {
                Id = 1,
                Name = "Laptop",
                Price = 50000m,
                IsActive = true
            }
        };

        var service = new OrderServiceClass(
            context,
            productClient,
            new FakePaymentServiceClient());

        await service.AddCartItemAsync(101, new AddCartItemDto
        {
            ProductId = 1,
            Quantity = 2
        });

        var result = await service.AddCartItemAsync(101, new AddCartItemDto
        {
            ProductId = 1,
            Quantity = 3
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(250000m, item.Subtotal);
        Assert.Equal(250000m, result.TotalAmount);
    }


    [Fact]
    public async Task CheckoutAsync_WhenCartIsEmpty_ThrowsInvalidOperationException()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var service = new OrderServiceClass(
            context,
            new FakeProductServiceClient(),
            new FakePaymentServiceClient());

        var dto = new CheckoutDto
        {
            ShippingAddress = "Bengaluru, Karnataka"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CheckoutAsync(101, dto));

        Assert.Equal("Cart is empty.", exception.Message);
    }

    [Fact]
    public async Task CheckoutAsync_WhenCartHasValidProduct_CreatesOrderAndClearsCart()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var cart = new ECommerce.OrderService.Models.Cart
        {
            UserId = 101,
            CartItems =
        {
            new ECommerce.OrderService.Models.CartItem
            {
                ProductId = 1,
                Quantity = 2
            }
        }
        };

        context.Carts.Add(cart);
        await context.SaveChangesAsync();

        var productClient = new FakeProductServiceClient
        {
            Product = new ProductResponse
            {
                Id = 1,
                Name = "Laptop",
                Price = 50000m,
                IsActive = true
            }
        };

        var service = new OrderServiceClass(
            context,
            productClient,
            new FakePaymentServiceClient());

        var dto = new CheckoutDto
        {
            ShippingAddress = "Bengaluru, Karnataka"
        };

        var result = await service.CheckoutAsync(101, dto);

        Assert.Equal(101, result.UserId);
        Assert.Equal(100000m, result.TotalAmount);
        Assert.Equal("Bengaluru, Karnataka", result.ShippingAddress);
        Assert.Equal(ECommerce.OrderService.Models.OrderStatus.Pending, result.Status);

        var orderItem = Assert.Single(result.Items);
        Assert.Equal("Laptop", orderItem.ProductName);
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(50000m, orderItem.UnitPrice);

        Assert.Empty(context.CartItems);
        Assert.Single(context.Orders);
    }


    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        var paymentClient = new FakePaymentServiceClient();

        var service = new OrderServiceClass(
            context,
            new FakeProductServiceClient(),
            paymentClient);

        var result = await service.ProcessPaymentAsync(101, 999, "Card");

        Assert.Null(result);
        Assert.Null(paymentClient.Payment);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderIsNotPending_ThrowsInvalidOperationException()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        context.Orders.Add(new ECommerce.OrderService.Models.Order
        {
            UserId = 101,
            Status = ECommerce.OrderService.Models.OrderStatus.Paid,
            TotalAmount = 100000m,
            ShippingAddress = "Bengaluru, Karnataka"
        });

        await context.SaveChangesAsync();

        var paymentClient = new FakePaymentServiceClient();

        var service = new OrderServiceClass(
            context,
            new FakeProductServiceClient(),
            paymentClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessPaymentAsync(101, 1, "Card"));

        Assert.Equal(
            "Payment can only be processed for pending orders.",
            exception.Message);

        Assert.Null(paymentClient.Payment);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentIsSuccessful_UpdatesOrderStatusToPaid()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new OrderDbContext(options);

        context.Orders.Add(new ECommerce.OrderService.Models.Order
        {
            UserId = 101,
            Status = ECommerce.OrderService.Models.OrderStatus.Pending,
            TotalAmount = 100000m,
            ShippingAddress = "Bengaluru, Karnataka"
        });

        await context.SaveChangesAsync();

        var paymentClient = new FakePaymentServiceClient
        {
            Payment = new PaymentResponse
            {
                Id = 1,
                OrderId = 1,
                Amount = 100000m,
                PaymentMethod = "Card",
                Status = 2
            }
        };

        var service = new OrderServiceClass(
            context,
            new FakeProductServiceClient(),
            paymentClient);

        var result = await service.ProcessPaymentAsync(101, 1, "Card");

        Assert.NotNull(result);
        Assert.Equal(2, result.Status);

        var order = await context.Orders.FindAsync(1);

        Assert.NotNull(order);
        Assert.Equal(
            ECommerce.OrderService.Models.OrderStatus.Paid,
            order.Status);
    }


}
