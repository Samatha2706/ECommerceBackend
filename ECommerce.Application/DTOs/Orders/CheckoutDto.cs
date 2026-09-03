using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Orders;

public class CheckoutDto
{
    [Required]
    [MaxLength(250)]
    public string ShippingAddress { get; set; } = string.Empty;
}