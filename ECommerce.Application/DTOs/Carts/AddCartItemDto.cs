using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Carts;

public class AddCartItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}