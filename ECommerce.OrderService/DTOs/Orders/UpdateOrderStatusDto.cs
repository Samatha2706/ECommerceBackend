using ECommerce.OrderService.Models;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.OrderService.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}