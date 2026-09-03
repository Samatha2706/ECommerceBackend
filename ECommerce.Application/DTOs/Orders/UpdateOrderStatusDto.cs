using ECommerce.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}