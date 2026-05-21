using MediatR;
using SharedLibrary.Responses;
using Application.DTOs;
using System.Collections.Generic;

namespace Application.Features.Warehouses.Queries.GetAllWarehouses
{
    public class GetAllWarehousesQuery : IRequest<ApiResponse<IEnumerable<WarehouseDTO>>>
    {
    }
}
