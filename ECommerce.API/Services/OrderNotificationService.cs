using ECommerce.API.Hubs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ECommerce.API.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderNotificationHub> _hubContext;

    public OrderNotificationService(
        IHubContext<OrderNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangedAsync(
        int userId,
        int orderId,
        string status)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync(
                "OrderStatusChanged",
                new
                {
                    orderId,
                    status,
                    message = $"Your order #{orderId} is now {status}."
                });
    }
}