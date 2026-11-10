
using ECommerce.PaymentService.Data;
using ECommerce.PaymentService.Models;
using ECommerce.PaymentService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.PaymentService.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;

    public PaymentService(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentDto> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        string paymentMethod)
    {
        if (orderId <= 0)
        {
            throw new ArgumentException(
                "Valid order ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            throw new ArgumentException(
                "Payment method is required.");
        }

        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (existingPayment != null &&
            existingPayment.Status == PaymentStatus.Successful)
        {
            return MapToDto(existingPayment);
        }

        Payment payment;

        if (existingPayment != null)
        {
            payment = existingPayment;
            payment.Amount = amount;
            payment.PaymentMethod = paymentMethod;
            payment.Status = PaymentStatus.Successful;
            payment.PaymentDate = DateTime.UtcNow;
        }
        else
        {
            payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Status = PaymentStatus.Successful,
                PaymentDate = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
        }

        payment.TransactionReference =
            $"MOCK-{Guid.NewGuid():N}";

        await _context.SaveChangesAsync();

        return MapToDto(payment);
    }

    public async Task<PaymentDto?> GetPaymentByOrderIdAsync(
        int orderId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        return payment == null
            ? null
            : MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionReference = payment.TransactionReference,
            PaymentDate = payment.PaymentDate
        };
    }
}
