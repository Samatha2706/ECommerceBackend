using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.BackgroundJobs;

public class InventoryMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryMonitorService> _logger;

    public InventoryMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Inventory monitor background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var context = scope.ServiceProvider
                    .GetRequiredService<ECommerceDbContext>();

                var lowStockProducts = await context.Inventories
                    .Include(inventory => inventory.Product)
                    .Where(inventory =>
                        inventory.Quantity <= inventory.ReorderLevel)
                    .ToListAsync(stoppingToken);

                foreach (var inventory in lowStockProducts)
                {
                    _logger.LogWarning(
                        "Low stock: Product {ProductName} has {Quantity} items remaining.",
                        inventory.Product.Name,
                        inventory.Quantity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while checking inventory.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(5),
                stoppingToken);
        }
    }
}