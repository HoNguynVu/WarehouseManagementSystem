using MediatR;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using SharedLibrary.Exceptions;

namespace Application.Features.Inventories.Commands.TransferInventory
{
    public class TransferInventoryCommandHandler : IRequestHandler<TransferInventoryCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IDistributedCache _cache;

        public TransferInventoryCommandHandler(IWarehouseUow warehouseUow, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _cache = cache;
        }

        public async Task<ApiResponse<bool>> Handle(TransferInventoryCommand request, CancellationToken cancellationToken)
        {
            var fromWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.FromWarehouseId);
            if (fromWarehouse == null)
                throw new NotFoundException("Không tìm thấy kho nguồn.");

            var inventoryItemA = fromWarehouse.Inventories.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (inventoryItemA == null || inventoryItemA.Quantity < request.Quantity)
                throw new BadRequestException("Kho nguồn không có sản phẩm này hoặc không đủ số lượng để chuyển.");

            var toWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.ToWarehouseId);
            if (toWarehouse == null)
                throw new NotFoundException("Không tìm thấy kho đích.");

            var currentUsedCapacityB = toWarehouse.Inventories.Sum(i => i.Quantity);
            if (currentUsedCapacityB + request.Quantity > toWarehouse.Capacity)
            {
                var remaining = toWarehouse.Capacity - currentUsedCapacityB;
                throw new BadRequestException($"Kho đích không đủ sức chứa! Chỉ còn trống: {remaining}");
            }

            inventoryItemA.Quantity -= request.Quantity;
            if (inventoryItemA.Quantity == 0)
            {
                fromWarehouse.Inventories.Remove(inventoryItemA);
            }

            var inventoryItemB = toWarehouse.Inventories.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (inventoryItemB != null)
            {
                inventoryItemB.Quantity += request.Quantity;
            }
            else
            {
                var newInventory = new Inventory
                {
                    Id = IdGenerator.GenerateId(ClassPrefix.Inventory),
                    ProductId = request.ProductId,
                    ProductName = inventoryItemA.ProductName,
                    Quantity = request.Quantity,
                    WarehouseId = request.ToWarehouseId
                };
                toWarehouse.Inventories.Add(newInventory);
            }

            var saved = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!saved)
            {
                _warehouseUow.ClearTracker();
                throw new BadRequestException("Lỗi hệ thống khi chuyển kho.");
            }

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.FromWarehouseId}", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.ToWarehouseId}", cancellationToken);
            return ApiResponse<bool>.Success(true, "Chuyển kho thành công!");
        }
    }
}
