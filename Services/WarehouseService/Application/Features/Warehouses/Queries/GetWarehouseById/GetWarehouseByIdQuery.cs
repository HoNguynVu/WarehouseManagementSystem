using MediatR;
using SharedLibrary.Responses;
using Application.DTOs;

namespace Application.Features.Warehouses.Queries.GetWarehouseById
{
    public class GetWarehouseByIdQuery : IRequest<ApiResponse<WarehouseDTO>>
    {
        public string Id { get; set; } = string.Empty;

        public GetWarehouseByIdQuery(string id)
        {
            Id = id;
        }

        public GetWarehouseByIdQuery() { }
    }
}
