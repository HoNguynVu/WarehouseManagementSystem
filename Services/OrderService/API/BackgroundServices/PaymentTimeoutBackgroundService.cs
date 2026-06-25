using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MassTransit;
using SharedLibrary.IntegrationEvents;

namespace API.BackgroundServices
{
    public class PaymentTimeoutBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentTimeoutBackgroundService> _logger;
        private readonly int _timeoutMinutes = 15;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public PaymentTimeoutBackgroundService(IServiceProvider serviceProvider, ILogger<PaymentTimeoutBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentTimeoutBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndCancelExpiredOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing PaymentTimeoutBackgroundService.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("PaymentTimeoutBackgroundService is stopping.");
        }

        private async Task CheckAndCancelExpiredOrdersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IOrderUow>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var expiredOrders = await uow.Orders.GetExpiredAwaitingPaymentOrdersAsync(_timeoutMinutes);

            var ordersList = expiredOrders.ToList();
            if (ordersList.Any())
            {
                _logger.LogInformation($"Found {ordersList.Count} expired awaiting payment orders. Initiating cancellation...");

                foreach (var order in ordersList)
                {
                    _logger.LogInformation($"Cancelling Order {order.Id} due to payment timeout.");
                    
                    // Publish OrderCancelledEvent to trigger Saga compensation
                    await publishEndpoint.Publish(new OrderCancelledEvent
                    {
                        OrderId = order.Id
                    }, stoppingToken);
                }
            }
        }
    }
}
