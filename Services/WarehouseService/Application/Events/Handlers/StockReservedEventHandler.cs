using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Handlers
{
    public class StockReservedEventHandler : INotificationHandler<StockReservedEvent>
    {
        private readonly ILogger<StockReservedEventHandler> _logger;

        public StockReservedEventHandler(ILogger<StockReservedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(StockReservedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Domain Event Handled: [StockReservedEvent] - Reserved {Quantity} units of Product {ProductId} in Warehouse {WarehouseId} for Order {OrderId}",
                notification.Quantity, notification.ProductId, notification.WarehouseId, notification.OrderId);

            return Task.CompletedTask;
        }
    }
}
