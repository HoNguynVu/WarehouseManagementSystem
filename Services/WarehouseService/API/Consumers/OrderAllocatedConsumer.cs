using Application.Events;
using Application.Interfaces;
using Application.DTOs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace API.Consumers
{
    public class OrderAllocatedConsumer : IConsumer<OrderAllocatedEvent>
    {
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<OrderAllocatedConsumer> _logger;
        public OrderAllocatedConsumer(IWarehouseService warehouseService, ILogger<OrderAllocatedConsumer> logger)
        {
            _warehouseService = warehouseService;
            _logger = logger;
        }
        public async Task Consume(ConsumeContext<OrderAllocatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("[RabbitMQ] Nhận yêu cầu giữ hàng cho Đơn {OrderId}", message.OrderId);

            // Tạo DTO gọi hàm Reserve
            var dto = new ReserveStockDTO
            {
                ProductId = message.ProductId,
                Quantity = message.Quantity,
                OrderId = message.OrderId
            };
            var result = await _warehouseService.ReserveStockAsync(message.WarehouseId, dto);

            if (result.IsSuccess)
                _logger.LogInformation("[RabbitMQ] Giữ hàng thành công cho Đơn {OrderId}", message.OrderId);
            else
                _logger.LogError("[RabbitMQ] Lỗi khi giữ hàng Đơn {OrderId}: {Error}", message.OrderId, result.Message);
        }
    }
}
