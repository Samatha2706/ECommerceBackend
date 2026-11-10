using ECommerce.PaymentService.DTOs;
using ECommerce.PaymentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> ProcessPayment(
        PaymentRequestDto dto,
        [FromQuery] decimal amount)
    {
        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(
                dto.OrderId,
                amount,
                dto.PaymentMethod);

            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByOrderId(
        int orderId)
    {
        var payment = await _paymentService
            .GetPaymentByOrderIdAsync(orderId);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}