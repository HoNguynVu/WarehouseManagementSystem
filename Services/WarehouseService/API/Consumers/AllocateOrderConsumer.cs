using MassTransit;
using MediatR;
using SharedLibrary.IntegrationEvents;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace API.Consumers
{
    public class AllocateOrderConsumer : IConsumer<AllocateOrderCommand>
    {
        private readonly ISender _sender;
        private readonly ILogger<AllocateOrderConsumer> _logger;

        public AllocateOrderConsumer(ISender sender, ILogger<AllocateOrderConsumer> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AllocateOrderCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Received AllocateOrderCommand for Order {OrderId}", message.OrderId);

            var command = new Application.Features.Orders.Commands.AllocateOrderCommand
            {
                OrderId = message.OrderId,
                Items = message.Items.Select(i => new Application.Features.Orders.Commands.OrderItemDto
                {
                    ProductId = i.ProductId,
                    RequiredQuantity = i.Quantity
                }).ToList()
            };

            var result = await _sender.Send(command, context.CancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to allocate stock for Order {OrderId}: {Message}", message.OrderId, result.Message);
            }
            else
            {
                _logger.LogInformation("Successfully allocated stock for Order {OrderId}", message.OrderId);
            }
        }
    }
}
