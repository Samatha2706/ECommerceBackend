using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Inventory;

public class UpdateInventoryDto
{
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }
}