using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Handlers
{
    public class InventoryAddedEventHandler : INotificationHandler<InventoryAddedEvent>
    {
        private readonly ILogger<InventoryAddedEventHandler> _logger;

        public InventoryAddedEventHandler(ILogger<InventoryAddedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(InventoryAddedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Domain Event Handled: [InventoryAddedEvent] - Product {ProductId} added to Warehouse {WarehouseId} with quantity {Quantity}",
                notification.ProductId, notification.WarehouseId, notification.Quantity);

            return Task.CompletedTask;
        }
    }
}
