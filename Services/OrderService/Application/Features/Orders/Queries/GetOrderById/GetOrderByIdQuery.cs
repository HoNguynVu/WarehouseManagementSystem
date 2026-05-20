using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQuery : IRequest<ApiResponse<OrderDTO>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
