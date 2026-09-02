using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Carts;

public class UpdateCartItemDto
{
    [Range(1, 1000)]
    public int Quantity { get; set; }
}