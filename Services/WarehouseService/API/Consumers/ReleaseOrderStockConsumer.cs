using MassTransit;
using MediatR;
using SharedLibrary.IntegrationEvents;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Consumers
{
    public class ReleaseOrderStockConsumer : IConsumer<ReleaseOrderStockCommand>
    {
        private readonly ISender _sender;
        private readonly ILogger<ReleaseOrderStockConsumer> _logger;

        public ReleaseOrderStockConsumer(ISender sender, ILogger<ReleaseOrderStockConsumer> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReleaseOrderStockCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Received ReleaseOrderStockCommand for Order {OrderId}", message.OrderId);

            var command = new Application.Features.Orders.Commands.ReleaseOrderCommand
            {
                OrderId = message.OrderId
            };

            var result = await _sender.Send(command, context.CancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to release stock for Order {OrderId}: {Message}", message.OrderId, result.Message);
            }
            else
            {
                _logger.LogInformation("Successfully released stock for Order {OrderId}", message.OrderId);
            }
        }
    }
}
