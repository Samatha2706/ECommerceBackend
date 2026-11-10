using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.OrderService.Models;

public class Order : BaseEntity
{
    public int UserId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

}
