namespace ECommerce.Application.DTOs.Products;

public class ProductPagedResultDto
{
    public IReadOnlyList<ProductDto> Products { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}