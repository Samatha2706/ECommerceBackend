using ECommerce.Application.DTOs.Categories;

namespace ECommerce.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateCategoryDto);

    Task<bool> DeleteAsync(int id);
}