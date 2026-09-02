using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync();

    Task<ProductDto?> GetByIdAsync(int id);

    Task<ProductDto> CreateAsync(CreateProductDto createProductDto);

    Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto updateProductDto);

    Task<bool> DeleteAsync(int id);
}