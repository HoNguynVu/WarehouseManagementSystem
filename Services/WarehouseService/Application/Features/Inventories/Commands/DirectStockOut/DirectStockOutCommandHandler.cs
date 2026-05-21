using MediatR;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.Inventories.Commands.DirectStockOut
{
    public class DirectStockOutCommandHandler : IRequestHandler<DirectStockOutCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IDistributedCache _cache;

        public DirectStockOutCommandHandler(IWarehouseUow warehouseUow, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _cache = cache;
        }

        public async Task<ApiResponse<bool>> Handle(DirectStockOutCommand request, CancellationToken cancellationToken)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.WarehouseId);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {request.WarehouseId}", 404);
            }
            var inventoryItem = existingWarehouse.Inventories.FirstOrDefault(i => i.ProductId == request.ProductId);
            
            if (inventoryItem == null)
                return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với mã: {request.ProductId} trong kho hàng.", 404);

            int availableQuantity = inventoryItem.Quantity - inventoryItem.ReservedQuantity;

            if (availableQuantity < request.Quantity)
            {
                return ApiResponse<bool>.Failure($"Không đủ hàng để xuất! Trong kho chỉ có thể xuất tối đa {availableQuantity} sản phẩm.", 400);
            }
            
            inventoryItem.Quantity -= request.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            
            if (inventoryItem.Quantity == 0)
            {
                existingWarehouse.Inventories.Remove(inventoryItem);
            }
            var saved = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!saved)
            {
                _warehouseUow.ClearTracker();
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi xuất hàng từ kho.", 500);
            }

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.WarehouseId}", cancellationToken);
            return ApiResponse<bool>.Success(true, "Xuất hàng từ kho thành công.");
        }
    }
}
