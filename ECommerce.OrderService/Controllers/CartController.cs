using System.Security.Claims;
using ECommerce.OrderService.DTOs.Carts;
using ECommerce.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IOrderService _orderService;

    public CartController(IOrderService orderService)
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

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        return Ok(await _orderService.GetCartAsync(userId.Value));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddCartItem(
        AddCartItemDto dto)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var cart = await _orderService.AddCartItemAsync(
                userId.Value, dto);

            return Ok(cart);
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

    [HttpPut("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(
        int cartItemId,
        UpdateCartItemDto dto)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        try
        {
            var cart = await _orderService.UpdateCartItemAsync(
                userId.Value, cartItemId, dto);

            if (cart == null)
                return NotFound();

            return Ok(cart);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{cartItemId}")]
    public async Task<ActionResult<CartDto>> RemoveCartItem(
        int cartItemId)
    {
        var userId = GetUserId();

        if (userId == null)
            return Unauthorized();

        var cart = await _orderService.RemoveCartItemAsync(
            userId.Value, cartItemId);

        if (cart == null)
            return NotFound();

        return Ok(cart);
    }
}