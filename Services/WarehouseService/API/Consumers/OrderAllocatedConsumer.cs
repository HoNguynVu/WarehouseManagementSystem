using Application.Events;
using Application.Interfaces;
using Application.DTOs;
using MassTransit;

namespace API.Consumers
{
    public class OrderAllocatedConsumer : IConsumer<OrderAllocatedEvent>
    {
        private readonly IWarehouseService _warehouseService;
        public OrderAllocatedConsumer(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }
        public async Task Consume(ConsumeContext<OrderAllocatedEvent> context)
        {
            var message = context.Message;
            Console.WriteLine($"[x] Bưu điện vừa nhận được Đơn hàng {message.OrderId} cần giữ {message.Quantity} sản phẩm {message.ProductId}");

            // Tạo DTO gọi hàm Reserve
            var dto = new ReserveStockDTO
            {
                ProductId = message.ProductId,
                Quantity = message.Quantity,
                OrderId = message.OrderId
            };
            var result = await _warehouseService.ReserveStockAsync(message.WarehouseId, dto);

            if (result.IsSuccess)
                Console.WriteLine($"[V] Đã giữ hàng thành công cho đơn {message.OrderId}!");
            else
                Console.WriteLine($"[X] Giữ hàng thất bại: {result.Message}");
        }
    }
}
