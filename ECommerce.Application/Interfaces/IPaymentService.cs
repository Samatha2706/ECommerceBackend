using ECommerce.Application.DTOs.Payments;

namespace ECommerce.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(
        int userId,
        PaymentRequestDto dto);
}