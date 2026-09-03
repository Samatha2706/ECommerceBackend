using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;

    public PaymentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentDto> ProcessPaymentAsync(
        int userId,
        PaymentRequestDto dto)
    {
        var order = await _context.Orders
            .Include(order => order.Payment)
            .FirstOrDefaultAsync(order =>
                order.Id == dto.OrderId &&
                order.UserId == userId);

        if (order is null)
        {
            throw new KeyNotFoundException(
                "Order not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                "Payment can only be processed for pending orders.");
        }

        if (order.Payment is not null)
        {
            throw new InvalidOperationException(
                "Payment has already been processed for this order.");
        }

        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
        {
            throw new ArgumentException(
                "Payment method is required.");
        }

        // Mock payment processing
        var paymentSuccessful = true;

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Status = paymentSuccessful
                ? PaymentStatus.Successful
                : PaymentStatus.Failed,
            PaymentMethod = dto.PaymentMethod.Trim(),
            TransactionReference = paymentSuccessful
                ? $"MOCK-{Guid.NewGuid():N}"
                : null,
            PaidAt = paymentSuccessful
                ? DateTime.UtcNow
                : null
        };

        if (paymentSuccessful)
        {
            order.Status = OrderStatus.Paid;
        }

        await _context.Payments.AddAsync(payment);

        await _context.SaveChangesAsync();

        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            PaymentDate = payment.PaidAt ?? DateTime.UtcNow
        };
    }
}