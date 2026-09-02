using ECommerce.Application.DTOs.Inventory;

namespace ECommerce.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryDto?> GetByProductIdAsync(int productId);

    Task<InventoryDto?> UpdateAsync(
        int productId,
        UpdateInventoryDto updateInventoryDto);

    Task<IReadOnlyList<InventoryDto>> GetLowStockAsync();
}