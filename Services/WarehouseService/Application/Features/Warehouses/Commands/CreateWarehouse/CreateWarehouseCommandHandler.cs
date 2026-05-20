using MediatR;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.Helpers;
using Microsoft.Extensions.Caching.Distributed;

namespace Application.Features.Warehouses.Commands.CreateWarehouse
{
    public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, ApiResponse<Warehouse>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public CreateWarehouseCommandHandler(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ApiResponse<Warehouse>> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
        {
            var warehouse = _mapper.Map<Warehouse>(request);
            
            warehouse.Id = IdGenerator.GenerateId(ClassPrefix.Warehouse);
            warehouse.CreatedAt = DateTime.UtcNow;

            await _warehouseUow.Warehouse.AddAsync(warehouse);
            var saved = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!saved)
            {
                _warehouseUow.ClearTracker();
                return ApiResponse<Warehouse>.Failure("Lỗi hệ thống khi tạo kho hàng.", 500);
            }
            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            return ApiResponse<Warehouse>.Success(warehouse, "Tạo kho hàng thành công.", 201);
        }
    }
}
