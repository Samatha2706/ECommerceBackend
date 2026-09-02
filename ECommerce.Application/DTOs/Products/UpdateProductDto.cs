using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Products;

public class UpdateProductDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public bool IsActive { get; set; }
}