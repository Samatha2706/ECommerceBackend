using System.Security.Claims;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

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

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(
        [FromBody] CheckoutDto dto)
    {
        try
        {
            var userId = GetUserId();

            var order = await _orderService.CheckoutAsync(
                userId,
                dto);

            return StatusCode(
                StatusCodes.Status201Created,
                order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders()
    {
        var userId = GetUserId();

        var orders = await _orderService.GetMyOrdersAsync(userId);

        return Ok(orders);
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDto>> GetById(
        int orderId)
    {
        var userId = GetUserId();

        var order = await _orderService.GetByIdAsync(
            userId,
            orderId);

        if (order is null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return Ok(order);
    }

    [HttpGet("admin/all")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAllOrders()
{
    var orders = await _orderService.GetAllOrdersAsync();

    return Ok(orders);
}

[HttpGet("admin/{orderId:int}")]
[Authorize(Roles = "Admin")]
public async Task<ActionResult<OrderDto>> GetOrderForAdmin(
    int orderId)
{
    var order = await _orderService.GetByIdForAdminAsync(orderId);

    if (order is null)
    {
        return NotFound(new
        {
            message = "Order not found."
        });
    }

    return Ok(order);
}

    [HttpPut("admin/{orderId:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(
    int orderId,
    [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _orderService.UpdateStatusAsync(
            orderId,
            dto);

        if (order is null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return Ok(order);
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