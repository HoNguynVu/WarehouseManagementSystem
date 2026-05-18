using MediatR;
using SharedLibrary.Responses;
using System.Collections.Generic;

namespace Application.Features.Orders
{
    // Dữ liệu đầu vào: Cần OrderId và danh sách món hàng
    public class AllocateOrderCommand : IRequest<ApiResponse<bool>>
    {
        public string OrderId { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }
    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public int RequiredQuantity { get; set; }
    }
}
