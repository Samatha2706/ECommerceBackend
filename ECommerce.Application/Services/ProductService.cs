using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Inventory> _inventoryRepository;

    public ProductService(
        IGenericRepository<Product> productRepository,
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Inventory> inventoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var inventories = await _inventoryRepository.GetAllAsync();

        return products
            .Select(product =>
            {
                var category = categories.FirstOrDefault(
                    c => c.Id == product.CategoryId);

                var inventory = inventories.FirstOrDefault(
                    i => i.ProductId == product.Id);

                return MapToDto(product, category, inventory);
            })
            .ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        var category = await _categoryRepository
            .GetByIdAsync(product.CategoryId);

        var inventories = await _inventoryRepository.GetAllAsync();

        var inventory = inventories.FirstOrDefault(
            i => i.ProductId == product.Id);

        return MapToDto(product, category, inventory);
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductDto createProductDto)
    {
        var name = createProductDto.Name.Trim();

        var category = await _categoryRepository
            .GetByIdAsync(createProductDto.CategoryId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "The specified category does not exist.");
        }

        var products = await _productRepository.GetAllAsync();

        var productNameExists = products.Any(product =>
            product.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase));

        if (productNameExists)
        {
            throw new InvalidOperationException(
                "A product with this name already exists.");
        }

        var product = new Product
        {
            Name = name,
            Description = createProductDto.Description?.Trim(),
            Price = createProductDto.Price,
            CategoryId = createProductDto.CategoryId,
            IsActive = true
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();

        var inventory = new Inventory
        {
            ProductId = product.Id,
            Quantity = createProductDto.InitialQuantity,
            ReorderLevel = createProductDto.ReorderLevel
        };

        await _inventoryRepository.AddAsync(inventory);
        await _inventoryRepository.SaveChangesAsync();

        return MapToDto(product, category, inventory);
    }

    public async Task<ProductDto?> UpdateAsync(
        int id,
        UpdateProductDto updateProductDto)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return null;
        }

        var category = await _categoryRepository
            .GetByIdAsync(updateProductDto.CategoryId);

        if (category is null)
        {
            throw new InvalidOperationException(
                "The specified category does not exist.");
        }

        var name = updateProductDto.Name.Trim();

        var products = await _productRepository.GetAllAsync();

        var productNameExists = products.Any(existingProduct =>
            existingProduct.Id != id &&
            existingProduct.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase));

        if (productNameExists)
        {
            throw new InvalidOperationException(
                "A product with this name already exists.");
        }

        product.Name = name;
        product.Description = updateProductDto.Description?.Trim();
        product.Price = updateProductDto.Price;
        product.CategoryId = updateProductDto.CategoryId;
        product.IsActive = updateProductDto.IsActive;

        _productRepository.Update(product);

        await _productRepository.SaveChangesAsync();

        var inventories = await _inventoryRepository.GetAllAsync();

        var inventory = inventories.FirstOrDefault(
            i => i.ProductId == product.Id);

        return MapToDto(product, category, inventory);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);

        if (product is null)
        {
            return false;
        }

        _productRepository.Delete(product);

        await _productRepository.SaveChangesAsync();

        return true;
    }

    private static ProductDto MapToDto(
        Product product,
        Category? category,
        Inventory? inventory)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            AvailableQuantity = inventory?.Quantity ?? 0
        };
    }
}