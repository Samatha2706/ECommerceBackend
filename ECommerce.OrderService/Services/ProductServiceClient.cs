using System.Net.Http.Json;

namespace ECommerce.OrderService.Services;

public class ProductServiceClient : IProductServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProductServiceClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ProductResponse?> GetProductByIdAsync(int productId)
    {
        var client = _httpClientFactory.CreateClient("ProductService");

        return await client.GetFromJsonAsync<ProductResponse>(
            $"api/Product/{productId}");
    }
}