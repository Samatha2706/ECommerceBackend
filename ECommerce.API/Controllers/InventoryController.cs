using ECommerce.Application.DTOs.Inventory;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    // GET: api/inventory/{productId}
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<InventoryDto>> GetByProductId(int productId)
    {
        var inventory = await _inventoryService
            .GetByProductIdAsync(productId);

        if (inventory is null)
        {
            return NotFound(new
            {
                message = "Product or inventory not found."
            });
        }

        return Ok(inventory);
    }

    // PUT: api/inventory/{productId}
    [HttpPut("{productId:int}")]
    public async Task<ActionResult<InventoryDto>> Update(
        int productId,
        [FromBody] UpdateInventoryDto updateInventoryDto)
    {
        var inventory = await _inventoryService.UpdateAsync(
            productId,
            updateInventoryDto);

        if (inventory is null)
        {
            return NotFound(new
            {
                message = "Product or inventory not found."
            });
        }

        return Ok(inventory);
    }

    // GET: api/inventory/low-stock
    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<InventoryDto>>> GetLowStock()
    {
        var inventory = await _inventoryService.GetLowStockAsync();

        return Ok(inventory);
    }
}