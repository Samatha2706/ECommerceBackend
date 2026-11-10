using ECommerce.ProductService.Data;
using ECommerce.ProductService.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ProductDbContext _context;

    public InventoryController(ProductDbContext context)
    {
        _context = context;
    }

    // GET: api/Inventory/{productId}
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<InventoryDto>> GetByProductId(int productId)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        if (inventory == null)
        {
            return NotFound(new
            {
                message = "Product or inventory not found."
            });
        }

        return Ok(new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product.Name,
            Quantity = inventory.Quantity,
            ReorderLevel = inventory.ReorderLevel,
            IsLowStock = inventory.Quantity <= inventory.ReorderLevel
        });
    }

    // PUT: api/Inventory/{productId}
    [HttpPut("{productId:int}")]
    public async Task<ActionResult<InventoryDto>> Update(
        int productId,
        [FromBody] UpdateInventoryDto dto)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);

        if (inventory == null)
        {
            return NotFound(new
            {
                message = "Product or inventory not found."
            });
        }

        inventory.Quantity = dto.Quantity;
        inventory.ReorderLevel = dto.ReorderLevel;

        await _context.SaveChangesAsync();

        return Ok(new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product.Name,
            Quantity = inventory.Quantity,
            ReorderLevel = inventory.ReorderLevel,
            IsLowStock = inventory.Quantity <= inventory.ReorderLevel
        });
    }

    // GET: api/Inventory/low-stock
    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<InventoryDto>>> GetLowStock()
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.Quantity <= i.ReorderLevel)
            .Select(i => new InventoryDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                ReorderLevel = i.ReorderLevel,
                IsLowStock = true
            })
            .ToListAsync();

        return Ok(inventory);
    }
}
