using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace ECommerce.OrderService.DTOs.Orders;
public class CheckoutDto
{
    [Required]
    [MaxLength(250)]
    public string ShippingAddress { get; set; } = string.Empty;
}