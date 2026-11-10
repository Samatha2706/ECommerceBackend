using System.ComponentModel.DataAnnotations;

namespace ECommerce.PaymentService.DTOs;

public class PaymentRequestDto
{
    [Required]
    public int OrderId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = string.Empty;
}