using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    Task<ProductPagedResultDto> GetAllAsync(
        string? search=null,
        int?categoryId=null,
        decimal?minPrice=null,
        decimal?maxPrice=null,
        string?sortBy=null,
        string?sortOrder=null,
        int pageNumber = 1,
        int pageSize = 10);

    Task<ProductDto?> GetByIdAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductDto createProductDto);

    Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto updateProductDto);

    Task<bool> DeleteAsync(int id);
}