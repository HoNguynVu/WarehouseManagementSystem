using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Commands.UpdateOrderStatus
{
    public class UpdateOrderStatusCommand : IRequest<ApiResponse<bool>>
    {
        public string OrderId { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}
