using MediatR;
using AutoMapper;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.Warehouses.Commands.UpdateWarehouse
{
    public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, ApiResponse<WarehouseDTO>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public UpdateWarehouseCommandHandler(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ApiResponse<WarehouseDTO>> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetByIdAsync(request.Id);
            if (existingWarehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {request.Id}", 404);
            }
            
            _mapper.Map(request, existingWarehouse);
            existingWarehouse.UpdatedAt = DateTime.UtcNow;
            _warehouseUow.Warehouse.Update(existingWarehouse);
            
            var updated = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!updated)
            {
                _warehouseUow.ClearTracker();
                return ApiResponse<WarehouseDTO>.Failure("Lỗi hệ thống khi cập nhật kho hàng.", 500);
            }
            
            var dto = _mapper.Map<WarehouseDTO>(existingWarehouse);

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.Id}", cancellationToken);
            
            return ApiResponse<WarehouseDTO>.Success(dto, "Cập nhật kho hàng thành công.");
        }
    }
}
