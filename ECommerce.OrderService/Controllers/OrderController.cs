
using System.Security.Claims;
using ECommerce.OrderService.DTOs.Orders;
using ECommerce.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private int? GetUserId()
    {
        var value =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        return int.TryParse(value, out var userId)
            ? userId
            : null;
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(CheckoutDto dto)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _orderService.CheckoutAsync(
                userId.Value, dto);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{orderId:int}/pay")]
    public async Task<IActionResult> ProcessPayment(
    int orderId,
    ProcessPaymentDto dto)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            return BadRequest(new { message = "Payment method is required." });

        try
        {
            var payment = await _orderService.ProcessPaymentAsync(
                userId.Value,
                orderId,
                dto.PaymentMethod);

            if (payment == null)
                return NotFound(new { message = "Order or payment not found." });

            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-orders")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders()
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var orders = await _orderService.GetMyOrdersAsync(userId.Value);

        return Ok(orders);
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int orderId)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var order = await _orderService.GetByIdAsync(
            userId.Value, orderId);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();

        return Ok(orders);
    }

    [HttpGet("admin/{orderId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> GetByIdForAdmin(int orderId)
    {
        var order = await _orderService.GetByIdForAdminAsync(orderId);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPut("admin/{orderId:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        int orderId,
        UpdateOrderStatusDto dto)
    {
        var order = await _orderService.UpdateStatusAsync(orderId, dto);

        if (order == null)
            return NotFound();

        return Ok(order);
    }
}
