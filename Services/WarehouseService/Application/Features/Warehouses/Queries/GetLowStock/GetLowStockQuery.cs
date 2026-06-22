using MediatR;
using SharedLibrary.Responses;
using Application.DTOs;
using System.Collections.Generic;

namespace Application.Features.Warehouses.Queries.GetLowStock
{
    public class GetLowStockQuery : IRequest<ApiResponse<IEnumerable<LowStockItemDTO>>>
    {
        public int Threshold { get; set; }

        public GetLowStockQuery(int threshold = 15)
        {
            Threshold = threshold;
        }
    }
}
