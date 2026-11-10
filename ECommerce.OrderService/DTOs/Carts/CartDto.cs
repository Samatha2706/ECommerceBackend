namespace ECommerce.OrderService.DTOs.Carts;

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}