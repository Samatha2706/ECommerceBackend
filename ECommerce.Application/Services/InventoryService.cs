using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IGenericRepository<Inventory> _inventoryRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public InventoryService(
        IGenericRepository<Inventory> inventoryRepository,
        IGenericRepository<Product> productRepository)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
    }

    public async Task<InventoryDto?> GetByProductIdAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);

        if (product is null)
        {
            return null;
        }

        var inventories = await _inventoryRepository.GetAllAsync();

        var inventory = inventories.FirstOrDefault(
            i => i.ProductId == productId);

        if (inventory is null)
        {
            return null;
        }

        return MapToDto(inventory, product);
    }

    public async Task<InventoryDto?> UpdateAsync(
        int productId,
        UpdateInventoryDto updateInventoryDto)
    {
        var product = await _productRepository.GetByIdAsync(productId);

        if (product is null)
        {
            return null;
        }

        var inventories = await _inventoryRepository.GetAllAsync();

        var inventory = inventories.FirstOrDefault(
            i => i.ProductId == productId);

        if (inventory is null)
        {
            return null;
        }

        inventory.Quantity = updateInventoryDto.Quantity;
        inventory.ReorderLevel = updateInventoryDto.ReorderLevel;

        _inventoryRepository.Update(inventory);

        await _inventoryRepository.SaveChangesAsync();

        return MapToDto(inventory, product);
    }

    public async Task<IReadOnlyList<InventoryDto>> GetLowStockAsync()
    {
        var inventories = await _inventoryRepository.GetAllAsync();
        var products = await _productRepository.GetAllAsync();

        var lowStockItems = inventories
            .Where(inventory =>
                inventory.Quantity <= inventory.ReorderLevel)
            .Select(inventory =>
            {
                var product = products.FirstOrDefault(
                    p => p.Id == inventory.ProductId);

                return MapToDto(inventory, product);
            })
            .Where(dto => dto.ProductId != 0)
            .ToList();

        return lowStockItems;
    }

    private static InventoryDto MapToDto(
        Inventory inventory,
        Product? product)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = product?.Name ?? string.Empty,
            Quantity = inventory.Quantity,
            ReorderLevel = inventory.ReorderLevel,
            IsLowStock = inventory.Quantity <= inventory.ReorderLevel
        };
    }
}