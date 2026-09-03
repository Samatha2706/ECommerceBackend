using System.ComponentModel.DataAnnotations;

namespace ECommerce.Application.DTOs.Payments;

public class PaymentRequestDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;
}