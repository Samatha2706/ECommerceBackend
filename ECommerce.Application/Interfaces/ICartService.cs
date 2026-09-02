using ECommerce.Application.DTOs.Carts;

namespace ECommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId);

    Task<CartDto> AddItemAsync(
        int userId,
        AddCartItemDto dto);

    Task<CartDto> UpdateItemAsync(
        int userId,
        int cartItemId,
        UpdateCartItemDto dto);

    Task<CartDto> RemoveItemAsync(
        int userId,
        int cartItemId);
}