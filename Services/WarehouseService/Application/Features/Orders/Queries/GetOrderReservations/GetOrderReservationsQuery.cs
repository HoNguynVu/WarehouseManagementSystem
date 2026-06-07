using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;
using System.Collections.Generic;

namespace Application.Features.Orders.Queries.GetOrderReservations
{
    public class GetOrderReservationsQuery : IRequest<ApiResponse<IEnumerable<StockReservationDTO>>>
    {
        public string OrderId { get; set; }

        public GetOrderReservationsQuery(string orderId)
        {
            OrderId = orderId;
        }
    }
}
