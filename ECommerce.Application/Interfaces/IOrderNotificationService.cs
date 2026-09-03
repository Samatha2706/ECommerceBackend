namespace ECommerce.Application.Interfaces;

public interface IOrderNotificationService
{
    Task NotifyOrderStatusChangedAsync(
        int userId,
        int orderId,
        string status);
}