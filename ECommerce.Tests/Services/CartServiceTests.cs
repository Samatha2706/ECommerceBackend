using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Services;

public class CartServiceTests
{
    private static ECommerceDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ECommerceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ECommerceDbContext(options);
    }

    private static async Task<Product> CreateProductAsync(
        ECommerceDbContext context,
        int quantity = 10)
    {
        var category = new Category
        {
            Name = "Electronics",
            Description = "Electronic products"
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test Product",
            Description = "Test product",
            Price = 100,
            CategoryId = category.Id,
            IsActive = true,
            Inventory = new Inventory
            {
                Quantity = quantity,
                ReorderLevel = 2
            }
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        return product;
    }

    [Fact]
    public async Task GetCartAsync_WhenCartDoesNotExist_CreatesEmptyCart()
    {
        await using var context = CreateDbContext();

        var service = new CartService(context);

        var result = await service.GetCartAsync(1);

        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalAmount);

        var cart = await context.Carts
            .FirstOrDefaultAsync(c => c.UserId == 1);

        Assert.NotNull(cart);
    }

    [Fact]
    public async Task AddItemAsync_WithValidProduct_AddsItemToCart()
    {
        await using var context = CreateDbContext();

        var product = await CreateProductAsync(context);

        var service = new CartService(context);

        var dto = new AddCartItemDto
        {
            ProductId = product.Id,
            Quantity = 2
        };

        var result = await service.AddItemAsync(1, dto);

        Assert.Single(result.Items);

        var item = result.Items.First();

        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(100, item.UnitPrice);
        Assert.Equal(200, item.Subtotal);
        Assert.Equal(200, result.TotalAmount);
    }

    [Fact]
    public async Task AddItemAsync_WhenAddingSameProductTwice_IncreasesQuantity()
    {
        await using var context = CreateDbContext();

        var product = await CreateProductAsync(context, 10);

        var service = new CartService(context);

        await service.AddItemAsync(
            1,
            new AddCartItemDto
            {
                ProductId = product.Id,
                Quantity = 2
            });

        var result = await service.AddItemAsync(
            1,
            new AddCartItemDto
            {
                ProductId = product.Id,
                Quantity = 3
            });

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items.First().Quantity);
        Assert.Equal(500, result.TotalAmount);
    }

    [Fact]
    public async Task AddItemAsync_WhenProductDoesNotExist_ThrowsException()
    {
        await using var context = CreateDbContext();

        var service = new CartService(context);

        var dto = new AddCartItemDto
        {
            ProductId = 999,
            Quantity = 1
        };

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AddItemAsync(1, dto));

        Assert.Equal(
            "Product not found or inactive.",
            exception.Message);
    }

    [Fact]
    public async Task AddItemAsync_WhenQuantityExceedsStock_ThrowsException()
    {
        await using var context = CreateDbContext();

        var product = await CreateProductAsync(context, 3);

        var service = new CartService(context);

        var dto = new AddCartItemDto
        {
            ProductId = product.Id,
            Quantity = 5
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddItemAsync(1, dto));

        Assert.Equal(
            "Requested quantity exceeds available stock.",
            exception.Message);
    }

    [Fact]
    public async Task UpdateItemAsync_WithValidQuantity_UpdatesCartItem()
    {
        await using var context = CreateDbContext();

        var product = await CreateProductAsync(context, 10);

        var service = new CartService(context);

        var cart = await service.AddItemAsync(
            1,
            new AddCartItemDto
            {
                ProductId = product.Id,
                Quantity = 2
            });

        var cartItemId = cart.Items.First().CartItemId;

        var result = await service.UpdateItemAsync(
            1,
            cartItemId,
            new UpdateCartItemDto
            {
                Quantity = 5
            });

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items.First().Quantity);
        Assert.Equal(500, result.TotalAmount);
    }

    [Fact]
    public async Task UpdateItemAsync_WhenCartItemDoesNotExist_ThrowsException()
    {
        await using var context = CreateDbContext();

        var service = new CartService(context);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateItemAsync(
                1,
                999,
                new UpdateCartItemDto
                {
                    Quantity = 2
                }));

        Assert.Equal(
            "Cart item not found.",
            exception.Message);
    }

    [Fact]
    public async Task RemoveItemAsync_WithExistingItem_RemovesItem()
    {
        await using var context = CreateDbContext();

        var product = await CreateProductAsync(context);

        var service = new CartService(context);

        var cart = await service.AddItemAsync(
            1,
            new AddCartItemDto
            {
                ProductId = product.Id,
                Quantity = 2
            });

        var cartItemId = cart.Items.First().CartItemId;

        var result = await service.RemoveItemAsync(
            1,
            cartItemId);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalAmount);

        var item = await context.CartItems
            .FirstOrDefaultAsync(i => i.Id == cartItemId);

        Assert.Null(item);
    }

    [Fact]
    public async Task RemoveItemAsync_WhenCartItemDoesNotExist_ThrowsException()
    {
        await using var context = CreateDbContext();

        var service = new CartService(context);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RemoveItemAsync(1, 999));

        Assert.Equal(
            "Cart item not found.",
            exception.Message);
    }
}