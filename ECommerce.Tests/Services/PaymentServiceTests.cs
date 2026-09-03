using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class PaymentServiceTests
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

    private static async Task CreateUserAsync(
        ECommerceDbContext context,
        int userId = 1)
    {
        context.Users.Add(new User
        {
            Id = userId,
            FullName = "Test User",
            Email = $"test{userId}@example.com",
            PasswordHash = "TestPasswordHash",
            Role = UserRole.Customer
        });

        await context.SaveChangesAsync();
    }

    private static async Task<Order> CreateOrderAsync(
        ECommerceDbContext context,
        int userId = 1,
        OrderStatus status = OrderStatus.Pending,
        decimal amount = 500)
    {
        var order = new Order
        {
            UserId = userId,
            TotalAmount = amount,
            ShippingAddress = "Chennai, Tamil Nadu",
            Status = status,
            OrderDate = DateTime.UtcNow
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order;
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidPendingOrder_CreatesSuccessfulPayment()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var order = await CreateOrderAsync(
                context,
                amount: 500);

            var service = new PaymentService(context);

            var result = await service.ProcessPaymentAsync(
                1,
                new PaymentRequestDto
                {
                    OrderId = order.Id,
                    PaymentMethod = "UPI"
                });

            Assert.NotNull(result);
            Assert.Equal(order.Id, result.OrderId);
            Assert.Equal(500, result.Amount);
            Assert.Equal(
                PaymentStatus.Successful,
                result.Status);
            Assert.Equal("UPI", result.PaymentMethod);
            Assert.NotNull(result.TransactionReference);
            Assert.StartsWith(
                "MOCK-",
                result.TransactionReference);

            var updatedOrder = await context.Orders
                .FirstAsync(o => o.Id == order.Id);

            Assert.Equal(
                OrderStatus.Paid,
                updatedOrder.Status);

            var payment = await context.Payments
                .FirstAsync(p => p.OrderId == order.Id);

            Assert.Equal(
                PaymentStatus.Successful,
                payment.Status);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderDoesNotExist_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var service = new PaymentService(context);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ProcessPaymentAsync(
                    1,
                    new PaymentRequestDto
                    {
                        OrderId = 999,
                        PaymentMethod = "UPI"
                    }));

            Assert.Equal(
                "Order not found.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderBelongsToAnotherUser_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context, 1);
            await CreateUserAsync(context, 2);

            var order = await CreateOrderAsync(
                context,
                userId: 1);

            var service = new PaymentService(context);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.ProcessPaymentAsync(
                    2,
                    new PaymentRequestDto
                    {
                        OrderId = order.Id,
                        PaymentMethod = "UPI"
                    }));

            Assert.Equal(
                "Order not found.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenOrderIsNotPending_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var order = await CreateOrderAsync(
                context,
                status: OrderStatus.Paid);

            var service = new PaymentService(context);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ProcessPaymentAsync(
                    1,
                    new PaymentRequestDto
                    {
                        OrderId = order.Id,
                        PaymentMethod = "UPI"
                    }));

            Assert.Equal(
                "Payment can only be processed for pending orders.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentAlreadyExists_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var order = await CreateOrderAsync(context);

            context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Status = PaymentStatus.Successful,
                PaymentMethod = "UPI",
                TransactionReference = "MOCK-EXISTING",
                PaidAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            var service = new PaymentService(context);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ProcessPaymentAsync(
                    1,
                    new PaymentRequestDto
                    {
                        OrderId = order.Id,
                        PaymentMethod = "UPI"
                    }));

            Assert.Equal(
                "Payment has already been processed for this order.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenPaymentMethodIsEmpty_ThrowsException()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var order = await CreateOrderAsync(context);

            var service = new PaymentService(context);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => service.ProcessPaymentAsync(
                    1,
                    new PaymentRequestDto
                    {
                        OrderId = order.Id,
                        PaymentMethod = "   "
                    }));

            Assert.Equal(
                "Payment method is required.",
                exception.Message);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_TrimsPaymentMethod()
    {
        var (context, connection) = await CreateDbContextAsync();

        try
        {
            await CreateUserAsync(context);

            var order = await CreateOrderAsync(context);

            var service = new PaymentService(context);

            var result = await service.ProcessPaymentAsync(
                1,
                new PaymentRequestDto
                {
                    OrderId = order.Id,
                    PaymentMethod = "  UPI  "
                });

            Assert.Equal("UPI", result.PaymentMethod);
        }
        finally
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}