using System.Security.Claims;
using ECommerce.Application.DTOs.Payments;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("process")]
    public async Task<ActionResult<PaymentDto>> ProcessPayment(
        [FromBody] PaymentRequestDto dto)
    {
        try
        {
            var userId = GetUserId();

            var payment = await _paymentService.ProcessPaymentAsync(
                userId,
                dto);

            return StatusCode(
                StatusCodes.Status201Created,
                payment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim is null ||
            !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "User ID could not be determined.");
        }

        return userId;
    }
}