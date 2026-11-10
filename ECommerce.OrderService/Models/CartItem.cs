namespace ECommerce.OrderService.Models;

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int ProductId { get; set; }


    public int Quantity { get; set; }
}