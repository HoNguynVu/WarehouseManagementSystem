using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrdersQuery : IRequest<ApiResponse<IEnumerable<OrderDTO>>>
    {
    }
}
