using System.Net.Http.Json;

namespace ECommerce.OrderService.Services;

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentServiceClient(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PaymentResponse?> ProcessPaymentAsync(
        int orderId,
        decimal amount,
        string paymentMethod)
    {
        var client = _httpClientFactory
            .CreateClient("PaymentService");

        var response = await client.PostAsJsonAsync(
            $"api/Payment?amount={amount}",
            new
            {
                orderId,
                paymentMethod
            });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<PaymentResponse>();
    }
}