using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ECommerce.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IGenericRepository<Product>> _productRepositoryMock;
    private readonly Mock<IGenericRepository<Category>> _categoryRepositoryMock;
    private readonly Mock<IGenericRepository<Inventory>> _inventoryRepositoryMock;
    private readonly IMemoryCache _cache;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock =
            new Mock<IGenericRepository<Product>>();

        _categoryRepositoryMock =
            new Mock<IGenericRepository<Category>>();

        _inventoryRepositoryMock =
            new Mock<IGenericRepository<Inventory>>();

        _cache = new MemoryCache(
            new MemoryCacheOptions());

        _productService = new ProductService(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _cache);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnProductsWithCategoryAndInventory()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Electronics"
        };

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 60000,
            CategoryId = 1,
            IsActive = true
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            Quantity = 10,
            ReorderLevel = 2
        };

        _productRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Product> { product });

        _categoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Category> { category });

        _inventoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Inventory> { inventory });

        // Act
        var result = await _productService.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Laptop", result[0].Name);
        Assert.Equal("Electronics", result[0].CategoryName);
        Assert.Equal(10, result[0].AvailableQuantity);
        Assert.Equal(60000, result[0].Price);
    }

    [Fact]
    public async Task GetAllAsync_ShouldUseCache_WhenProductsAreAlreadyCached()
    {
        // Arrange
        var cachedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = 1,
                Name = "Cached Laptop",
                Price = 60000,
                CategoryId = 1,
                CategoryName = "Electronics",
                AvailableQuantity = 5,
                IsActive = true
            }
        };

        _cache.Set("products_all", cachedProducts);

        // Act
        var result = await _productService.GetAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Cached Laptop", result[0].Name);

        _productRepositoryMock.Verify(
            repository => repository.GetAllAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 60000,
            CategoryId = 1,
            IsActive = true
        };

        var category = new Category
        {
            Id = 1,
            Name = "Electronics"
        };

        var inventory = new Inventory
        {
            Id = 1,
            ProductId = 1,
            Quantity = 10,
            ReorderLevel = 2
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        _inventoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Inventory> { inventory });

        // Act
        var result = await _productService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Electronics", result.CategoryName);
        Assert.Equal(10, result.AvailableQuantity);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProductAndInventory()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "  Laptop  ",
            Description = "  Gaming laptop  ",
            Price = 60000,
            CategoryId = 1,
            InitialQuantity = 10,
            ReorderLevel = 2
        };

        var category = new Category
        {
            Id = 1,
            Name = "Electronics"
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        _productRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Product>()))
            .Callback<Product>(product => product.Id = 1)
            .Returns(Task.CompletedTask);

        _productRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        _inventoryRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Inventory>()))
            .Returns(Task.CompletedTask);

        _inventoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _productService.CreateAsync(dto);

        // Assert
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Gaming laptop", result.Description);
        Assert.Equal(60000, result.Price);
        Assert.Equal("Electronics", result.CategoryName);
        Assert.Equal(10, result.AvailableQuantity);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Product>(product =>
                    product.Name == "Laptop" &&
                    product.CategoryId == 1 &&
                    product.Price == 60000 &&
                    product.IsActive)),
            Times.Once);

        _inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Inventory>(inventory =>
                    inventory.ProductId == 1 &&
                    inventory.Quantity == 10 &&
                    inventory.ReorderLevel == 2)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCategoryDoesNotExist()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "Laptop",
            Price = 60000,
            CategoryId = 999,
            InitialQuantity = 10,
            ReorderLevel = 2
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(999))
            .ReturnsAsync((Category?)null);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _productService.CreateAsync(dto));

        Assert.Equal(
            "The specified category does not exist.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Product>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenProductNameAlreadyExists()
    {
        // Arrange
        var dto = new CreateProductDto
        {
            Name = "laptop",
            Price = 60000,
            CategoryId = 1,
            InitialQuantity = 10,
            ReorderLevel = 2
        };

        var category = new Category
        {
            Id = 1,
            Name = "Electronics"
        };

        var existingProduct = new Product
        {
            Id = 1,
            Name = "Laptop",
            CategoryId = 1
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        _productRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Product>
            {
                existingProduct
            });

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _productService.CreateAsync(dto));

        Assert.Equal(
            "A product with this name already exists.",
            exception.Message);

        _productRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Product>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenProductExists()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Laptop"
        };

        _productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _productService.DeleteAsync(1);

        // Assert
        Assert.True(result);

        _productRepositoryMock.Verify(
            repository => repository.Delete(product),
            Times.Once);

        _productRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }
}