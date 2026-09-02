using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities;
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Inventory Inventory { get; set; } = null!;
}