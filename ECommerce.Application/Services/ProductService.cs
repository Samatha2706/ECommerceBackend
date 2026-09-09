using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IGenericRepository<Inventory> _inventoryRepository;

    private readonly IMemoryCache _cache;


    public ProductService(
        IGenericRepository<Product> productRepository,
        IGenericRepository<Category> categoryRepository,
        IGenericRepository<Inventory> inventoryRepository,
        IMemoryCache cache)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _inventoryRepository = inventoryRepository;
        _cache = cache;
    }

    public async Task<ProductPagedResultDto> GetAllAsync(
    string? search = null, 
    int? categoryId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? sortBy = null,
    string? sortOrder = null,
    int pageNumber = 1,
    int pageSize = 10)
    {
        var products = await _productRepository.GetAllAsync();
        var categories = await _categoryRepository.GetAllAsync();
        var inventories = await _inventoryRepository.GetAllAsync();


        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            products = products
                .Where(product =>
                    product.Name.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    (product.Description != null &&
                     product.Description.Contains(
                         search,
                         StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (categoryId.HasValue)
        {
            products = products
                .Where(p => p.CategoryId == categoryId.Value)
                .ToList();
        }
        if (minPrice.HasValue)
        {
            products = products
                .Where(product => product.Price >= minPrice.Value)
                .ToList();
        }

        if (maxPrice.HasValue)
        {
            products = products
                .Where(product => product.Price <= maxPrice.Value)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            sortBy = sortBy.Trim().ToLowerInvariant();
            sortOrder = sortOrder?.Trim().ToLowerInvariant();

            var descending = sortOrder == "desc";

            products = sortBy switch
            {
                "price" => descending
                    ? products.OrderByDescending(p => p.Price).ToList()
                    : products.OrderBy(p => p.Price).ToList(),

                "name" => descending
                    ? products.OrderByDescending(p => p.Name).ToList()
                    : products.OrderBy(p => p.Name).ToList(),

                _ => products
            };
        }

        

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }

        var totalCount = products.Count;

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        var pagedProducts = products
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = pagedProducts
            .Select(product =>
            {
                var category = categories.FirstOrDefault(
                    c => c.Id == product.CategoryId);

                var inventory = inventories.FirstOrDefault(
                    i => i.ProductId == product.Id);

                return MapToDto(product, category, inventory);
            })
            .ToList();

        return new ProductPagedResultDto
        {
            Products = result,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
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

        _cache.Remove("products_all");

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
        _cache.Remove("products_all");

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
        _cache.Remove("products_all");

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
            AvailableQuantity = inventory?.Quantity ?? 0,
            ReorderLevel = inventory?.ReorderLevel ?? 0
        };
    }
}