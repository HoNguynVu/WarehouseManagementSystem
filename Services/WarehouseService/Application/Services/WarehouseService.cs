using Application.Interfaces;
using Application.DTOs;
using Domain.Interfaces;
using SharedLibrary.Responses;
using AutoMapper;

namespace Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IMapper _mapper;
        public WarehouseService(IWarehouseRepository warehouseRepository, IMapper mapper)
        {
            _warehouseRepository = warehouseRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<Domain.Entities.Warehouse>> CreateWarehouseAsync(CreateWarehouseDTO warehouseDto)
        {
            var warehouse = _mapper.Map<Domain.Entities.Warehouse>(warehouseDto);

            warehouse.Id = Guid.NewGuid().ToString();
            warehouse.CreatedAt = DateTime.UtcNow;

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
            var dtos = _mapper.Map<IEnumerable<WarehouseDTO>>(warehouses);
            return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtos, "Lấy danh sách kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> GetWarehouseByIdAsync(string id)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id);
            if (warehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}");
            }
            var dto = _mapper.Map<WarehouseDTO>(warehouse);
            return ApiResponse<WarehouseDTO>.Success(dto, "Lấy thông tin kho hàng thành công.");
        }
    }
}
