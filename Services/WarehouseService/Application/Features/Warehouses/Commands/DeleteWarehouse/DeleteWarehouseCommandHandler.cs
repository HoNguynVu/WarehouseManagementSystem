using MediatR;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;
using SharedLibrary.Exceptions;

namespace Application.Features.Warehouses.Commands.DeleteWarehouse
{
    public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IDistributedCache _cache;

        public DeleteWarehouseCommandHandler(IWarehouseUow warehouseUow, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _cache = cache;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetByIdAsync(request.Id);
            if (existingWarehouse == null)
            {
                throw new NotFoundException($"Không tìm thấy kho hàng với ID: {request.Id}");
            }

            if (existingWarehouse.Inventories != null && existingWarehouse.Inventories.Any())
            {
                throw new BadRequestException("Không thể xóa kho hàng vì còn tồn kho bên trong. Vui lòng xuất hết hàng trước khi xóa.");
            }

            _warehouseUow.Warehouse.Delete(existingWarehouse);
            var deleted = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!deleted)
            {
                _warehouseUow.ClearTracker();
                throw new BadRequestException("Lỗi hệ thống khi xóa kho hàng.");
            }

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.Id}", cancellationToken);
            return ApiResponse<bool>.Success(true, "Xóa kho hàng thành công.");
        }
    }
}
