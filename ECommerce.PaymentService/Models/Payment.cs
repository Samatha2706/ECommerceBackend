using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.PaymentService.Models;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = "Mock";
    public string? TransactionReference { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}
