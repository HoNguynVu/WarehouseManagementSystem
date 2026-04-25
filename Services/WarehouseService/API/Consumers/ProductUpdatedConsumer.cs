using Domain.Interfaces;
using MassTransit;
using SharedLibrary.IntergrationEvents;

namespace API.Consumers
{
    public class ProductUpdatedConsumer : IConsumer<UpdateProductEvent>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly ILogger<ProductUpdatedConsumer> _logger;
        public ProductUpdatedConsumer(IWarehouseUow warehouseUow, ILogger<ProductUpdatedConsumer> logger)
        {
            _warehouseUow = warehouseUow;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<UpdateProductEvent> context)
        {
            var message = context.Message;
            
            var allWarehouses = await _warehouseUow.Warehouse.GetWarehousesContainingProductAsync(message.ProductId);

            bool hasChanges = false;

            foreach (var warehouse in allWarehouses)
            {
                var inventories = warehouse.Inventories.Where(i => i.ProductId == message.ProductId);
                foreach (var inventory in inventories) 
                {
                    inventory.ProductName = message.ProductName;
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _warehouseUow.Warehouse.SaveChangeAsync();
                _logger.LogInformation(
                    "[RabbitMQ] Đã đồng bộ tên sản phẩm {ProductId} thành '{ProductName}' trong kho!",
                    message.ProductId,
                    message.ProductName);
            }
        }
    }
}
