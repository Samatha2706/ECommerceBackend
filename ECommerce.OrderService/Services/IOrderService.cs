using ECommerce.OrderService.DTOs.Carts;
using ECommerce.OrderService.DTOs.Orders;

namespace ECommerce.OrderService.Services;

public interface IOrderService
{
    Task<CartDto> GetCartAsync(int userId);

    Task<CartDto> AddCartItemAsync(
        int userId,
        AddCartItemDto dto);

    Task<CartDto?> UpdateCartItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemDto dto);

    Task<CartDto?> RemoveCartItemAsync(
        int userId,
        int cartItemId);

    Task<OrderDto> CheckoutAsync(int userId, CheckoutDto dto);

    Task<PaymentResponse?> ProcessPaymentAsync(
    int userId,
    int orderId,
    string paymentMethod);

    Task<OrderDto?> GetByIdAsync(int userId, int orderId);

    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int userId);

    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync();

    Task<OrderDto?> GetByIdForAdminAsync(int orderId);

    Task<OrderDto?> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusDto dto);
}