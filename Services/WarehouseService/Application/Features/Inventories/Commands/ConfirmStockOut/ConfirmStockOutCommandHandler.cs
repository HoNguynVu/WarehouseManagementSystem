using MediatR;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.Inventories.Commands.ConfirmStockOut
{
    public class ConfirmStockOutCommandHandler : IRequestHandler<ConfirmStockOutCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IDistributedCache _cache;

        public ConfirmStockOutCommandHandler(IWarehouseUow warehouseUow, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _cache = cache;
        }

        public async Task<ApiResponse<bool>> Handle(ConfirmStockOutCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.WarehouseId);
                if (warehouse == null) throw new Exception("Không tìm thấy kho hàng.");

                var inventoryItem = warehouse.Inventories.FirstOrDefault(i => i.ProductId == request.ProductId);
                if (inventoryItem == null) throw new Exception("Không tìm thấy sản phẩm trong kho hàng.");

                if (inventoryItem.ReservedQuantity < request.Quantity)
                {
                    throw new Exception($"Số lượng cần xuất vượt quá số lượng đang giữ! Đang giữ: {inventoryItem.ReservedQuantity}, Cần xuất: {request.Quantity}");
                }

                inventoryItem.Quantity -= request.Quantity;
                inventoryItem.ReservedQuantity -= request.Quantity;

                if (inventoryItem.Quantity == 0)
                {
                    warehouse.Inventories.Remove(inventoryItem);
                }

                await _warehouseUow.SaveChangeAsync(cancellationToken);
                await _cache.RemoveAsync("all_warehouses", cancellationToken);
                await _cache.RemoveAsync($"warehouse_{request.WarehouseId}", cancellationToken);
                return ApiResponse<bool>.Success(true, "Xác nhận xuất hàng thành công!");
            }
            catch (Exception ex)
            {
                _warehouseUow.ClearTracker();
                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
        }
    }
}
