namespace ECommerce.OrderService.Services;

public interface IPaymentServiceClient
{
    Task<PaymentResponse?> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        string paymentMethod);
}

public class PaymentResponse
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public int Status { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime PaymentDate { get; set; }
}