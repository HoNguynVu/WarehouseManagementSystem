using MediatR;
using AutoMapper;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Application.Features.Warehouses.Queries.GetWarehouseById
{
    public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, ApiResponse<WarehouseDTO>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public GetWarehouseByIdQueryHandler(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ApiResponse<WarehouseDTO>> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = $"warehouse_{request.Id}";

            var cacheData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cacheData))
            {
                var dtoFromCache = JsonSerializer.Deserialize<WarehouseDTO>(cacheData);
                return ApiResponse<WarehouseDTO>.Success(dtoFromCache, "Lấy thông tin kho hàng thành công (từ cache).");
            }

            var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.Id);
            if (warehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {request.Id}", 404);
            }
            var dto = _mapper.Map<WarehouseDTO>(warehouse);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions, cancellationToken);
            return ApiResponse<WarehouseDTO>.Success(dto, "Lấy thông tin kho hàng thành công.");
        }
    }
}
