using MediatR;
using SharedLibrary.Responses;
using Application.DTOs;
using Domain.Interfaces;

namespace Application.Features.Warehouses.Queries.GetLowStock
{
    public class GetLowStockQueryHandler : IRequestHandler<GetLowStockQuery, ApiResponse<IEnumerable<LowStockItemDTO>>>
    {
        private readonly IWarehouseUow _uow;

        public GetLowStockQueryHandler(IWarehouseUow uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<IEnumerable<LowStockItemDTO>>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
        {
            var inventories = await _uow.Warehouse.GetLowStockAsync(request.Threshold);
            var lowStockItems = inventories.Select(i => new LowStockItemDTO
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                WarehouseId = i.WarehouseId,
                WarehouseName = i.Warehouse?.Name ?? "Unknown",
                Quantity = i.Quantity
            });
            
            return new ApiResponse<IEnumerable<LowStockItemDTO>> { Data = lowStockItems, IsSuccess = true };
        }
    }
}
