using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.PaymentService.Models
{
    public enum PaymentStatus
    {
        Pending = 1,
        Successful = 2,
        Failed = 3,
        Refunded = 4
    }
}
