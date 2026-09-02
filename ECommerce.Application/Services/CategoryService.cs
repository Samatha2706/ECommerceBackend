using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IGenericRepository<Category> _categoryRepository;

    public CategoryService(IGenericRepository<Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories
            .Select(MapToDto)
            .ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        return category is null ? null : MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto)
    {
        var categories = await _categoryRepository.GetAllAsync();

        var categoryNameExists = categories.Any(category =>
            category.Name.Equals(
                createCategoryDto.Name.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (categoryNameExists)
        {
            throw new InvalidOperationException("A category with this name already exists.");
        }

        var category = new Category
        {
            Name = createCategoryDto.Name.Trim(),
            Description = createCategoryDto.Description?.Trim()
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return MapToDto(category);
    }


    public async Task<CategoryDto?> UpdateAsync(
    int id,
    UpdateCategoryDto updateCategoryDto)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category is null)
        {
            return null;
        }

        var categories = await _categoryRepository.GetAllAsync();

        var categoryNameExists = categories.Any(existingCategory =>
            existingCategory.Id != id &&
            existingCategory.Name.Equals(
                updateCategoryDto.Name.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (categoryNameExists)
        {
            throw new InvalidOperationException(
                "A category with this name already exists.");
        }

        category.Name = updateCategoryDto.Name.Trim();
        category.Description = updateCategoryDto.Description?.Trim();

        _categoryRepository.Update(category);

        await _categoryRepository.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category is null)
        {
            return false;
        }

        _categoryRepository.Delete(category);

        await _categoryRepository.SaveChangesAsync();

        return true;
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };
    }
}