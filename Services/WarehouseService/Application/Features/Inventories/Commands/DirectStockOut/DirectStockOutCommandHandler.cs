using MediatR;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;
using SharedLibrary.Exceptions;

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
                throw new NotFoundException($"Không tìm thấy kho hàng với ID: {request.WarehouseId}");
            }
            var inventoryItem = existingWarehouse.Inventories.FirstOrDefault(i => i.ProductId == request.ProductId);
            
            if (inventoryItem == null)
                throw new NotFoundException($"Không tìm thấy sản phẩm với mã: {request.ProductId} trong kho hàng.");

            int availableQuantity = inventoryItem.Quantity - inventoryItem.ReservedQuantity;

            if (availableQuantity < request.Quantity)
            {
                throw new BadRequestException($"Không đủ hàng để xuất! Trong kho chỉ có thể xuất tối đa {availableQuantity} sản phẩm.");
            }
            
            inventoryItem.Quantity -= request.Quantity;
            
            if (inventoryItem.Quantity == 0)
            {
                existingWarehouse.Inventories.Remove(inventoryItem);
            }
            var saved = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!saved)
            {
                _warehouseUow.ClearTracker();
                throw new BadRequestException("Lỗi hệ thống khi xuất hàng từ kho.");
            }

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.WarehouseId}", cancellationToken);
            return ApiResponse<bool>.Success(true, "Xuất hàng từ kho thành công.");
        }
    }
}
