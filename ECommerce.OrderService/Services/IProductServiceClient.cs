namespace ECommerce.OrderService.Services;

public interface IProductServiceClient
{
    Task<ProductResponse?> GetProductByIdAsync(int productId);
}

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}