using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Payments;

public class PaymentDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime PaymentDate { get; set; }
}