using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;

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
                return ApiResponse<Domain.Entities.Warehouse>.Failure("Lỗi hệ thống khi tạo kho hàng.");
            }
            return ApiResponse<Domain.Entities.Warehouse>.Success(warehouse, "Tạo kho hàng thành công.");
        }

        public async Task<ApiResponse<IEnumerable<WarehouseDTO>>> GetAllWarehousesAsync()
        {
            var warehouses = await _warehouseRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<WarehouseDTO>>(warehouses);
            return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtos, "Lấy danh sách kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> GetWarehouseByIdAsync(string id)
        {
            var warehouse = await _warehouseRepository.GetWarehouseWithInventoriesAsync(id);
            if (warehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}");
            }
            var dto = _mapper.Map<WarehouseDTO>(warehouse);
            return ApiResponse<WarehouseDTO>.Success(dto, "Lấy thông tin kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> UpdateWarehouseAsync(string id, UpdateWarehouseDTO warehouseDto)
        {
            var existingWarehouse = await _warehouseRepository.GetByIdAsync(id);
            if (existingWarehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}");
            }
            _mapper.Map(warehouseDto, existingWarehouse);
            existingWarehouse.UpdatedAt = DateTime.UtcNow;
            _warehouseRepository.Update(existingWarehouse);
            var updated = await _warehouseRepository.SaveChangeAsync();
            if (!updated)
            {
                return ApiResponse<WarehouseDTO>.Failure("Lỗi hệ thống khi cập nhật kho hàng.");
            }
            var dto = _mapper.Map<WarehouseDTO>(existingWarehouse);
            return ApiResponse<WarehouseDTO>.Success(dto, "Cập nhật kho hàng thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteWarehouseAsync(string id)
        {
            var existingWarehouse = await _warehouseRepository.GetByIdAsync(id);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {id}");
            }
            _warehouseRepository.Delete(existingWarehouse);
            var deleted = await _warehouseRepository.SaveChangeAsync();
            if (!deleted)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi xóa kho hàng.");
            }
            return ApiResponse<bool>.Success(true, "Xóa kho hàng thành công.");
        }
        public async Task<ApiResponse<bool>> AddInventoryToWarehouseAsync(string warehouseId, AddInventoryDTO inventoryDto)
        {
            var existingWarehouse = await _warehouseRepository.GetWarehouseWithInventoriesAsync(warehouseId);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {warehouseId}");
            }
            var currentUsedCapacity = existingWarehouse.Inventories.Sum(i => i.Quantity);

            if (currentUsedCapacity + inventoryDto.Quantity > existingWarehouse.Capacity)
            {
                var remaining = existingWarehouse.Capacity - currentUsedCapacity;
                return ApiResponse<bool>.Failure($"Kho đã đầy! Sức chứa còn lại chỉ là: {remaining}");
            }

            var newInventory = _mapper.Map<Inventory>(inventoryDto);
            newInventory.Id = Guid.NewGuid().ToString();
            newInventory.CreatedAt = DateTime.UtcNow;
            newInventory.WarehouseId = warehouseId;

            existingWarehouse.Inventories.Add(newInventory);
            var saved = await _warehouseRepository.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi thêm hàng vào kho.");
            }
            return ApiResponse<bool>.Success(true, "Nhập hàng vào kho thành công.");
        }

        public async Task<ApiResponse<bool>> StockOutAsync(string warehouseId, StockOutDTO stockOutDto)
        {
            var existingWarehouse = await _warehouseRepository.GetWarehouseWithInventoriesAsync(warehouseId);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {warehouseId}");
            }
            var inventoryItem = existingWarehouse.Inventories.FirstOrDefault(i => i.ProductId == stockOutDto.ProductId);
            // Kiểm tra tồn tại sản phẩm trong kho
            if (inventoryItem == null)
                return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với mã: {stockOutDto.ProductId} trong kho hàng.");
            // Kiểm tra đủ số lượng để xuất
            if (inventoryItem.Quantity < stockOutDto.Quantity)
            {
                return ApiResponse<bool>.Failure($"Không đủ hàng để xuất! Trong kho chỉ còn lại {inventoryItem.Quantity} sản phẩm.");
            }
            // Thực hiện xuất hàng
            inventoryItem.Quantity -= stockOutDto.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            // Nếu số lượng sau khi xuất hàng bằng 0, ta sẽ xóa luôn mục tồn kho đó để tránh lưu trữ những mục không còn giá trị
            if (inventoryItem.Quantity == 0)
            {
                existingWarehouse.Inventories.Remove(inventoryItem);
            }
            var saved = await _warehouseRepository.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi xuất hàng từ kho.");
            }
            return ApiResponse<bool>.Success(true, "Xuất hàng từ kho thành công.");
        }
    }
}
