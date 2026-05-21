using Domain.Enums;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;

namespace API.Consumers
{
    public class InventoryAllocationFailedConsumer : IConsumer<InventoryAllocationFailedEvent>
    {
        private readonly IOrderUow _orderUow;
        private readonly ILogger<InventoryAllocationFailedConsumer> _logger;

        public InventoryAllocationFailedConsumer(IOrderUow orderUow, ILogger<InventoryAllocationFailedConsumer> logger)
        {
            _orderUow = orderUow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<InventoryAllocationFailedEvent> context)
        {
            var message = context.Message;
            var order = await _orderUow.Orders.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("[RabbitMQ] Order not found: {OrderId}", message.OrderId);
                return;
            }
            // Idempotency: bỏ qua nếu đã Completed hoặc Failed
            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Failed)
            {
                _logger.LogInformation("[RabbitMQ] Order {OrderId} already Completed or Failed, skipping.", message.OrderId);
                return;
            }
            try
            {
                await _orderUow.BeginTransactionAsync();

                order.Status = OrderStatus.Failed;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                _orderUow.Orders.Update(order);
                await _orderUow.CommitAsync();
                _logger.LogInformation("[RabbitMQ] Order {OrderId} marked as Failed due to inventory allocation failure.", message.OrderId);
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
