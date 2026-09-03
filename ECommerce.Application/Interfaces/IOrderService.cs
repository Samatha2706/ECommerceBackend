using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(
        int userId,
        CheckoutDto dto);

    Task<OrderDto?> GetByIdAsync(
        int userId,
        int orderId);

    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(
        int userId);

    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync();

    Task<OrderDto?> GetByIdForAdminAsync(int orderId);

    Task<OrderDto?> UpdateStatusAsync(
    int orderId,
    UpdateOrderStatusDto dto);
}