using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities;
public class Inventory : BaseEntity
{
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; } = 5;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}