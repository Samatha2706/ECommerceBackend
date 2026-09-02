using ECommerce.Application.DTOs.Carts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;

    public CartService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(
        int userId,
        AddCartItemDto dto)
    {
        var product = await _context.Products
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product =>
                product.Id == dto.ProductId &&
                product.IsActive);

        if (product is null)
        {
            throw new KeyNotFoundException(
                "Product not found or inactive.");
        }

        if (product.Inventory is null)
        {
            throw new InvalidOperationException(
                "Product inventory is not configured.");
        }

        var cart = await GetOrCreateCartAsync(userId);

        var existingItem = cart.CartItems
            .FirstOrDefault(item =>
                item.ProductId == dto.ProductId);

        var newQuantity = existingItem is null
            ? dto.Quantity
            : existingItem.Quantity + dto.Quantity;

        if (newQuantity > product.Inventory.Quantity)
        {
            throw new InvalidOperationException(
                "Requested quantity exceeds available stock.");
        }

        if (existingItem is null)
        {
            cart.CartItems.Add(new CartItem
            {
                ProductId = product.Id,
                Quantity = dto.Quantity
            });
        }
        else
        {
            existingItem.Quantity = newQuantity;
        }

        await _context.SaveChangesAsync();

        return MapToDto(cart);
    }

    public async Task<CartDto> UpdateItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemDto dto)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.CartItems
            .FirstOrDefault(item => item.Id == cartItemId);

        if (item is null)
        {
            throw new KeyNotFoundException(
                "Cart item not found.");
        }

        var product = await _context.Products
            .Include(product => product.Inventory)
            .FirstOrDefaultAsync(product =>
                product.Id == item.ProductId);

        if (product is null || !product.IsActive)
        {
            throw new InvalidOperationException(
                "Product is no longer available.");
        }

        if (product.Inventory is null ||
            dto.Quantity > product.Inventory.Quantity)
        {
            throw new InvalidOperationException(
                "Requested quantity exceeds available stock.");
        }

        item.Quantity = dto.Quantity;

        await _context.SaveChangesAsync();

        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(
        int userId,
        int cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.CartItems
            .FirstOrDefault(item => item.Id == cartItemId);

        if (item is null)
        {
            throw new KeyNotFoundException(
                "Cart item not found.");
        }

        _context.CartItems.Remove(item);

        await _context.SaveChangesAsync();

        return MapToDto(cart);
    }

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(cart =>
                cart.UserId == userId);

        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            UserId = userId
        };

        await _context.Carts.AddAsync(cart);
        await _context.SaveChangesAsync();

        return cart;
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.CartItems.Select(item =>
            new CartItemDto
            {
                CartItemId = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity,
                Subtotal = item.Product.Price * item.Quantity
            })
            .ToList();

        return new CartDto
        {
            CartId = cart.Id,
            Items = items,
            TotalAmount = items.Sum(item => item.Subtotal)
        };
    }
}