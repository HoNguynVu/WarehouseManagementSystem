using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Queries.GetOrderState
{
    public class GetOrderStateQuery : IRequest<ApiResponse<OrderStateDTO>>
    {
        public string OrderId { get; set; }

        public GetOrderStateQuery(string orderId)
        {
            OrderId = orderId;
        }
    }
}
