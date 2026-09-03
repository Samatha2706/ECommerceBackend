using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using Moq;

namespace ECommerce.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<IGenericRepository<Category>> _categoryRepositoryMock;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock =
            new Mock<IGenericRepository<Category>>();

        _categoryService = new CategoryService(
            _categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new Category
            {
                Id = 1,
                Name = "Electronics",
                Description = "Electronic products"
            },
            new Category
            {
                Id = 2,
                Name = "Clothing",
                Description = "Clothing products"
            }
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryService.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Electronics", result[0].Name);
        Assert.Equal("Clothing", result[1].Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenCategoryExists()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic products"
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Electronic products", result.Description);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCategoryDoesNotExist()
    {
        // Arrange
        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(999))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _categoryService.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategory()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "  Electronics  ",
            Description = "  Electronic products  "
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        _categoryRepositoryMock
            .Setup(repository => repository.AddAsync(
                It.IsAny<Category>()))
            .Returns(Task.CompletedTask);

        _categoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _categoryService.CreateAsync(dto);

        // Assert
        Assert.Equal("Electronics", result.Name);
        Assert.Equal("Electronic products", result.Description);

        _categoryRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Category>(category =>
                    category.Name == "Electronics" &&
                    category.Description == "Electronic products")),
            Times.Once);

        _categoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenCategoryNameAlreadyExists()
    {
        // Arrange
        var dto = new CreateCategoryDto
        {
            Name = "electronics",
            Description = "Another description"
        };

        var existingCategory = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Existing category"
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Category>
            {
                existingCategory
            });

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _categoryService.CreateAsync(dto));

        Assert.Equal(
            "A category with this name already exists.",
            exception.Message);

        _categoryRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.IsAny<Category>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Old Name",
            Description = "Old description"
        };

        var dto = new UpdateCategoryDto
        {
            Name = "  New Name  ",
            Description = "  New description  "
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<Category>
            {
                category
            });

        _categoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _categoryService.UpdateAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New description", result.Description);

        _categoryRepositoryMock.Verify(
            repository => repository.Update(category),
            Times.Once);

        _categoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenCategoryExists()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Description = "Electronic products"
        };

        _categoryRepositoryMock
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _categoryService.DeleteAsync(1);

        // Assert
        Assert.True(result);

        _categoryRepositoryMock.Verify(
            repository => repository.Delete(category),
            Times.Once);

        _categoryRepositoryMock.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once);
    }
}