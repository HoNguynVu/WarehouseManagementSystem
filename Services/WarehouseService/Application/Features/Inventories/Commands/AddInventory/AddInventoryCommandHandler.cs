using MediatR;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.DTOs;
using Application.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Net.Http;
using SharedLibrary.Exceptions;
using Domain.Events;

namespace Application.Features.Inventories.Commands.AddInventory
{
    public class AddInventoryCommandHandler : IRequestHandler<AddInventoryCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;

        public AddInventoryCommandHandler(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache, IHttpClientFactory httpClientFactory)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ApiResponse<bool>> Handle(AddInventoryCommand request, CancellationToken cancellationToken)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(request.WarehouseId);
            if (existingWarehouse == null)
            {
                throw new NotFoundException($"Không tìm thấy kho hàng với ID: {request.WarehouseId}");
            }
            var currentUsedCapacity = existingWarehouse.Inventories.Sum(i => i.Quantity);

            if (currentUsedCapacity + request.Quantity > existingWarehouse.Capacity)
            {
                var remaining = existingWarehouse.Capacity - currentUsedCapacity;
                throw new BadRequestException($"Kho đã đầy! Sức chứa còn lại chỉ là: {remaining}");
            }

            var client = _httpClientFactory.CreateClient("CatalogClient");
            var response = await client.GetAsync($"/api/Product/{request.ProductId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new BadRequestException($"Không tìm thấy sản phẩm với mã: {request.ProductId} trong hệ thống.");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var catalogResult = JsonSerializer.Deserialize<ApiResponse<CatalogProductDTO>>(responseString, jsonOptions);
            if (catalogResult == null || !catalogResult.IsSuccess || catalogResult.Data == null)
            {
                throw new BadRequestException("Lỗi khi đọc dữ liệu từ Catalog Service.");
            }

            string productId = catalogResult.Data.Id;
            string productName = catalogResult.Data.Name;

            var newInventory = _mapper.Map<Inventory>(request);
            newInventory.Id = IdGenerator.GenerateId(ClassPrefix.Inventory);
            newInventory.ProductName = productName;
            newInventory.ProductId = productId;
            newInventory.WarehouseId = request.WarehouseId;

            // Đăng ký Domain Event
            newInventory.AddDomainEvent(new InventoryAddedEvent(productId, request.WarehouseId, request.Quantity));

            existingWarehouse.Inventories.Add(newInventory);
            var saved = await _warehouseUow.SaveChangeAsync(cancellationToken);
            if (!saved)
            {
                _warehouseUow.ClearTracker();
                throw new BadRequestException("Lỗi hệ thống khi thêm hàng vào kho.");
            }

            await _cache.RemoveAsync("all_warehouses", cancellationToken);
            await _cache.RemoveAsync($"warehouse_{request.WarehouseId}", cancellationToken);
            return ApiResponse<bool>.Success(true, "Nhập hàng vào kho thành công.");
        }
    }
}
