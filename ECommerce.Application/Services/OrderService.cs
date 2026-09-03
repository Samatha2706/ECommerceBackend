using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;

    public OrderService(IApplicationDbContext context,
        IOrderNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private readonly IOrderNotificationService _notificationService;

    public async Task<OrderDto> CheckoutAsync(
        int userId,
        CheckoutDto dto)
    {
        var shippingAddress = dto.ShippingAddress.Trim();

        if (string.IsNullOrWhiteSpace(shippingAddress))
        {
            throw new ArgumentException(
                "Shipping address is required.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await _context.BeginTransactionAsync();
            try
            {
                var cart = await _context.Carts
                    .Include(cart => cart.CartItems)
                    .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Inventory)
                    .FirstOrDefaultAsync(cart =>
                        cart.UserId == userId);

                if (cart is null || !cart.CartItems.Any())
                {
                    throw new InvalidOperationException(
                        "Your cart is empty.");
                }

                decimal totalAmount = 0;

                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;

                    if (!product.IsActive)
                    {
                        throw new InvalidOperationException(
                            $"Product '{product.Name}' is no longer available.");
                    }

                    if (product.Inventory is null)
                    {
                        throw new InvalidOperationException(
                            $"Inventory is not configured for '{product.Name}'.");
                    }

                    if (item.Quantity > product.Inventory.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.Name}'.");
                    }

                    totalAmount += product.Price * item.Quantity;
                }

                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    ShippingAddress = shippingAddress,
                    Status = OrderStatus.Pending,
                    OrderDate = DateTime.UtcNow
                };

                await _context.Orders.AddAsync(order);

                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;

                    var orderItem = new OrderItem
                    {
                        Order = order,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    };

                    await _context.OrderItems.AddAsync(orderItem);

                    product.Inventory.Quantity -= item.Quantity;
                }

                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return MapToDto(order);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        });
    }

    public async Task<OrderDto?> GetByIdAsync(
        int userId,
        int orderId)
    {
        var order = await _context.Orders
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order =>
                order.Id == orderId &&
                order.UserId == userId);

        return order is null
            ? null
            : MapToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(
        int userId)
    {
        var orders = await _context.Orders
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .Where(order => order.UserId == userId)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();

        return orders
            .Select(MapToDto)
            .ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .OrderByDescending(order => order.OrderDate)
            .ToListAsync();

        return orders
            .Select(MapToDto)
            .ToList();
    }

    public async Task<OrderDto?> GetByIdForAdminAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order =>
                order.Id == orderId);

        return order is null
            ? null
            : MapToDto(order);
    }

    public async Task<OrderDto?> UpdateStatusAsync(
    int orderId,
    UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders
            .Include(order => order.OrderItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order =>
                order.Id == orderId);

        if (order is null)
        {
            return null;
        }

        order.Status = dto.Status;

        await _context.SaveChangesAsync();

        await _notificationService.NotifyOrderStatusChangedAsync(
            order.UserId,
            order.Id,
            order.Status.ToString());

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status,
            CreatedAt = order.OrderDate,
            Items = order.OrderItems
                .Select(item => new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Subtotal =
                        item.UnitPrice * item.Quantity
                })
                .ToList()
        };
    }
}