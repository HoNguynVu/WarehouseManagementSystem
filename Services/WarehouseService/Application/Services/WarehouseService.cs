using Application.Interfaces;
using Application.DTOs;
using Domain.Interfaces;
using SharedLibrary.Responses;
using System.Xml;

namespace Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;
        public WarehouseService(IWarehouseRepository warehouseRepository)
        {
            _warehouseRepository = warehouseRepository;
        }

        public async Task<ApiResponse<Domain.Entities.Warehouse>> CreateWarehouseAsync(CreateWarehouseDTO warehouseDto)
        {
            var warehouse = new Domain.Entities.Warehouse
            {
                Id = Guid.NewGuid().ToString(),
                Name = warehouseDto.Name,
                Address = warehouseDto.Address,
                Capacity = warehouseDto.Capacity,
                CreatedAt = DateTime.UtcNow,
            };
            await _warehouseRepository.AddAsync(warehouse);
            var saved = await _warehouseRepository.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<Domain.Entities.Warehouse>.Failure("Failed to create warehouse.");
            }
            return ApiResponse<Domain.Entities.Warehouse>.Success(warehouse, "Warehouse created successfully.");
        }

        public async Task<ApiResponse<IEnumerable<WarehouseDTO>>> GetAllWarehousesAsync()
        {
            var warehouses = await _warehouseRepository.GetAllAsync();
            var dtos = warehouses.Select(w => new WarehouseDTO
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                Capacity = w.Capacity,
                CreatedAt = w.CreatedAt
            }).ToList();
            return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtos, "Lấy danh sách kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> GetWarehouseByIdAsync(string id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}");
            }
            var dto = new WarehouseDTO
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                Address = warehouse.Address,
                Capacity = warehouse.Capacity,
                CreatedAt = warehouse.CreatedAt
            };
            return ApiResponse<WarehouseDTO>.Success(dto, "Lấy thông tin kho hàng thành công.");
        }
    }
}
