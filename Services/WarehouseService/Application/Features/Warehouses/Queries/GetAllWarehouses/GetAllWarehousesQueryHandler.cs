using MediatR;
using AutoMapper;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.DTOs;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Collections.Generic;

namespace Application.Features.Warehouses.Queries.GetAllWarehouses
{
    public class GetAllWarehousesQueryHandler : IRequestHandler<GetAllWarehousesQuery, ApiResponse<IEnumerable<WarehouseDTO>>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public GetAllWarehousesQueryHandler(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<ApiResponse<IEnumerable<WarehouseDTO>>> Handle(GetAllWarehousesQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = "all_warehouses";

            var cacheData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cacheData))
            {
                var dtosFromCache = JsonSerializer.Deserialize<IEnumerable<WarehouseDTO>>(cacheData);
                return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtosFromCache, "Lấy danh sách kho hàng thành công (từ cache).");
            }

            var warehouses = await _warehouseUow.Warehouse.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<WarehouseDTO>>(warehouses);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), cacheOptions, cancellationToken);

            return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtos, "Lấy danh sách kho hàng thành công.");
        }
    }
}
