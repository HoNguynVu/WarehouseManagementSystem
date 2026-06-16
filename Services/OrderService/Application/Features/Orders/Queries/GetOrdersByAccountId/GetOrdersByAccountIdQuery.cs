using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;
using System.Collections.Generic;

namespace Application.Features.Orders.Queries.GetOrdersByAccountId
{
    public class GetOrdersByAccountIdQuery : IRequest<ApiResponse<IEnumerable<OrderDTO>>>
    {
        public string AccountId { get; set; } = string.Empty;

        public GetOrdersByAccountIdQuery(string accountId)
        {
            AccountId = accountId;
        }
    }
}
