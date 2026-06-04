using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommand : IRequest<ApiResponse<bool>>
    {
        public string OrderId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
    }
}
