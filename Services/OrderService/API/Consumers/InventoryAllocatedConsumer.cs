using Domain.Enums;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;

namespace API.Consumers
{
    public class InventoryAllocatedConsumer : IConsumer<InventoryAllocatedEvent>
    {
        private readonly IOrderUow _orderUow;
        private readonly ILogger<InventoryAllocatedConsumer> _logger;

        public InventoryAllocatedConsumer(IOrderUow orderUow, ILogger<InventoryAllocatedConsumer> logger)
        {
            _orderUow = orderUow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<InventoryAllocatedEvent> context)
        {
            var message = context.Message;

            var order = await _orderUow.Orders.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("[RabbitMQ] Order not found: {OrderId}", message.OrderId);
                return;
            }

            // Idempotency: bỏ qua nếu đã Completed
            if (order.Status == OrderStatus.Completed)
            {
                _logger.LogInformation("[RabbitMQ] Order {OrderId} already Completed, skipping.", message.OrderId);
                return;
            }

            try
            {
                await _orderUow.BeginTransactionAsync();

                order.Status = OrderStatus.Completed;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                _orderUow.Orders.Update(order);

                await _orderUow.CommitAsync();

                _logger.LogInformation("[RabbitMQ] Order {OrderId} marked as Completed.", message.OrderId);
            }
            catch (Exception ex)
            {
                await _orderUow.RollbackAsync();
                _logger.LogError(ex, "[RabbitMQ] Error updating order status for OrderId: {OrderId}", message.OrderId);
                throw;
            }
        }
    }
}
