using ECommerce.OrderService.Data;
using ECommerce.OrderService.DTOs.Orders;
using ECommerce.OrderService.Models;
using Microsoft.EntityFrameworkCore;
using ECommerce.OrderService.DTOs.Carts;

namespace ECommerce.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IProductServiceClient _productServiceClient;

    private readonly IPaymentServiceClient _paymentServiceClient;
    public OrderService(
        OrderDbContext context,
        IProductServiceClient productServiceClient,
        IPaymentServiceClient paymentServiceClient)
    {
        _context = context;
        _productServiceClient = productServiceClient;
        _paymentServiceClient = paymentServiceClient;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return new CartDto();
        }

        return await MapCartToDtoAsync(cart);
    }

    public async Task<CartDto> AddCartItemAsync(
        int userId,
        AddCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var product = await _productServiceClient
            .GetProductByIdAsync(dto.ProductId);

        if (product == null || !product.IsActive)
            throw new InvalidOperationException("Product is unavailable.");

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _context.Carts.Add(cart);
        }

        var existingItem = cart.CartItems
            .FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            cart.CartItems.Add(new CartItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity
            });
        }

        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task<CartDto?> UpdateCartItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemDto dto)
    {
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == cartItemId &&
                i.Cart.UserId == userId);

        if (item == null)
            return null;

        item.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    public async Task<CartDto?> RemoveCartItemAsync(
        int userId,
        int cartItemId)
    {
        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == cartItemId &&
                i.Cart.UserId == userId);

        if (item == null)
            return null;

        _context.CartItems.Remove(item);

        await _context.SaveChangesAsync();

        return await GetCartAsync(userId);
    }

    private async Task<CartDto> MapCartToDtoAsync(Cart cart)
    {
        var cartDto = new CartDto();

        foreach (var item in cart.CartItems)
        {
            var product = await _productServiceClient
                .GetProductByIdAsync(item.ProductId);

            var unitPrice = product?.Price ?? 0;

            var cartItemDto = new CartItemDto
            {
                CartItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = product?.Name ?? "Unavailable product",
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                Subtotal = unitPrice * item.Quantity
            };

            cartDto.Items.Add(cartItemDto);
            cartDto.TotalAmount += cartItemDto.Subtotal;
        }

        return cartDto;
    }

    public async Task<OrderDto> CheckoutAsync(
        int userId,
        CheckoutDto dto)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        var order = new Order
        {
            UserId = userId,
            ShippingAddress = dto.ShippingAddress,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 0
        };

        foreach (var cartItem in cart.CartItems)
        {
            var product = await _productServiceClient
                .GetProductByIdAsync(cartItem.ProductId);

            if (product == null || !product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Product {cartItem.ProductId} is unavailable.");
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price
            };

            order.OrderItems.Add(orderItem);

            order.TotalAmount += product.Price * cartItem.Quantity;
        }

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        _context.CartItems.RemoveRange(cart.CartItems);

        await _context.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<PaymentResponse?> ProcessPaymentAsync(
    int userId,
    int orderId,
    string paymentMethod)
    {
        // Find the order belonging to this customer
        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                o.UserId == userId);

        if (order == null)
        {
            return null;
        }

        // Payment is allowed only for Pending orders
        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                "Payment can only be processed for pending orders.");
        }

        // Process payment through PaymentService
        var payment = await _paymentServiceClient.ProcessPaymentAsync(
            order.Id,
            order.TotalAmount,
            paymentMethod);

        if (payment == null)
        {
            return null;
        }

        // PaymentStatus.Successful = 2
        if (payment.Status == 2)
        {
            order.Status = OrderStatus.Paid;

            await _context.SaveChangesAsync();
        }

        return payment;
    }

    public async Task<OrderDto?> GetByIdAsync(
        int userId,
        int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(
                o => o.Id == orderId && o.UserId == userId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(
        int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdForAdminAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return null;
        }

        order.Status = dto.Status;

        await _context.SaveChangesAsync();

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
            Items = order.OrderItems.Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.UnitPrice * item.Quantity
            }).ToList()
        };
    }
}