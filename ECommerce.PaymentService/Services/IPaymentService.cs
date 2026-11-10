using ECommerce.PaymentService.DTOs;

namespace ECommerce.PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        string paymentMethod);

    Task<PaymentDto?> GetPaymentByOrderIdAsync(int orderId);
}