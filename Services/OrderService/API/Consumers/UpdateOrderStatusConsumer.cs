using Domain.Enums;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;
using System;
using System.Threading.Tasks;

namespace API.Consumers
{
    public class UpdateOrderStatusConsumer : IConsumer<UpdateOrderStatusCommand>
    {
        private readonly IOrderUow _orderUow;
        private readonly ILogger<UpdateOrderStatusConsumer> _logger;

        public UpdateOrderStatusConsumer(IOrderUow orderUow, ILogger<UpdateOrderStatusConsumer> logger)
        {
            _orderUow = orderUow;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<UpdateOrderStatusCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Updating Order {OrderId} status to {Status}. Reason: {Reason}", message.OrderId, message.Status, message.Reason);

            var order = await _orderUow.Orders.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", message.OrderId);
                return;
            }

            try
            {
                await _orderUow.BeginTransactionAsync();
                order.Status = message.Status;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                _orderUow.Orders.Update(order);
                await _orderUow.CommitAsync();
                _logger.LogInformation("Successfully updated Order {OrderId} status to {Status}", message.OrderId, message.Status);
            }
            catch (Exception ex)
            {
                await _orderUow.RollbackAsync();
                _logger.LogError(ex, "Failed to update Order {OrderId} status to {Status}", message.OrderId, message.Status);
                throw;
            }
        }
    }
}
