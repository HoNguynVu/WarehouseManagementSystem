using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Application.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;


namespace Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        public WarehouseService(IWarehouseUow warehouseUow, IMapper mapper, IDistributedCache cache, IHttpClientFactory httpClientFactory)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _cache = cache;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<ApiResponse<Warehouse>> CreateWarehouseAsync(CreateWarehouseDTO warehouseDto)
        {
            var warehouse = _mapper.Map<Warehouse>(warehouseDto);
            
            warehouse.Id = IdGenerator.GenerateId(ClassPrefix.Warehouse);
            warehouse.CreatedAt = DateTime.UtcNow;

            await _warehouseUow.Warehouse.AddAsync(warehouse);
            var saved = await _warehouseUow.Warehouse.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<Warehouse>.Failure("Lỗi hệ thống khi tạo kho hàng.", 500);
            }
            await _cache.RemoveAsync("all_warehouses");
            return ApiResponse<Warehouse>.Success(warehouse, "Tạo kho hàng thành công.", 201);
        }

        public async Task<ApiResponse<IEnumerable<WarehouseDTO>>> GetAllWarehousesAsync()
        {
            string cacheKey = "all_warehouses";

            var cacheData = await _cache.GetStringAsync(cacheKey);
            if(!string.IsNullOrEmpty(cacheData))
            {
                var dtosFromCache = JsonSerializer.Deserialize<IEnumerable<WarehouseDTO>>(cacheData);
                return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtosFromCache, "Lấy danh sách kho hàng thành công (từ cache).");
            }

            var warehouses = await _warehouseUow.Warehouse.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<WarehouseDTO>>(warehouses);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Cho phép cache tồn tại trong 10 phút
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), cacheOptions);

            return ApiResponse<IEnumerable<WarehouseDTO>>.Success(dtos, "Lấy danh sách kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> GetWarehouseByIdAsync(string id)
        {
            string cacheKey = $"warehouse_{id}";

            var cacheData = await _cache.GetStringAsync(cacheKey);
            if(!string.IsNullOrEmpty(cacheData))
            {
                var dtoFromCache = JsonSerializer.Deserialize<WarehouseDTO>(cacheData);
                return ApiResponse<WarehouseDTO>.Success(dtoFromCache, "Lấy thông tin kho hàng thành công (từ cache).");
            }

            var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(id);
            if (warehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}", 404);
            }
            var dto = _mapper.Map<WarehouseDTO>(warehouse);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Cho phép cache tồn tại trong 10 phút
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);
            return ApiResponse<WarehouseDTO>.Success(dto, "Lấy thông tin kho hàng thành công.");
        }

        public async Task<ApiResponse<WarehouseDTO>> UpdateWarehouseAsync(string id, UpdateWarehouseDTO warehouseDto)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetByIdAsync(id);
            if (existingWarehouse == null)
            {
                return ApiResponse<WarehouseDTO>.Failure($"Không tìm thấy kho hàng với ID: {id}", 404);
            }
            _mapper.Map(warehouseDto, existingWarehouse);
            existingWarehouse.UpdatedAt = DateTime.UtcNow;
            _warehouseUow.Warehouse.Update(existingWarehouse);
            var updated = await _warehouseUow.Warehouse.SaveChangeAsync();
            if (!updated)
            {
                return ApiResponse<WarehouseDTO>.Failure("Lỗi hệ thống khi cập nhật kho hàng.", 500);
            }
            var dto = _mapper.Map<WarehouseDTO>(existingWarehouse);

            await _cache.RemoveAsync("all_warehouses");
            await _cache.RemoveAsync($"warehouse_{id}");
            return ApiResponse<WarehouseDTO>.Success(dto, "Cập nhật kho hàng thành công.");
        }

        public async Task<ApiResponse<bool>> DeleteWarehouseAsync(string id)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetByIdAsync(id);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {id}", 404);
            }

            if (existingWarehouse != null && existingWarehouse.Inventories.Any())
            {
                return ApiResponse<bool>.Failure("Không thể xóa kho hàng vì còn tồn kho bên trong. Vui lòng xuất hết hàng trước khi xóa.", 400);
            }

            _warehouseUow.Warehouse.Delete(existingWarehouse);
            var deleted = await _warehouseUow.Warehouse.SaveChangeAsync();
            if (!deleted)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi xóa kho hàng.", 500);
            }

            await _cache.RemoveAsync("all_warehouses");
            await _cache.RemoveAsync($"warehouse_{id}");
            return ApiResponse<bool>.Success(true, "Xóa kho hàng thành công.");
        }
        public async Task<ApiResponse<bool>> AddInventoryToWarehouseAsync(string warehouseId, AddInventoryDTO inventoryDto)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(warehouseId);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {warehouseId}", 404);
            }
            var currentUsedCapacity = existingWarehouse.Inventories.Sum(i => i.Quantity);

            if (currentUsedCapacity + inventoryDto.Quantity > existingWarehouse.Capacity)
            {
                var remaining = existingWarehouse.Capacity - currentUsedCapacity;
                return ApiResponse<bool>.Failure($"Kho đã đầy! Sức chứa còn lại chỉ là: {remaining}", 400);
            }

            var client = _httpClientFactory.CreateClient("CatalogClient");
            var response = await client.GetAsync($"/api/Product/{inventoryDto.ProductId}");
            if(!response.IsSuccessStatusCode)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với mã: {inventoryDto.ProductId} trong hệ thống.", 400);
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var catalogResult = JsonSerializer.Deserialize<ApiResponse<CatalogProductDTO>>(responseString, jsonOptions);
            if (catalogResult == null || !catalogResult.IsSuccess || catalogResult.Data == null)
            {
                return ApiResponse<bool>.Failure("Lỗi khi đọc dữ liệu từ Catalog Service.", 500);
            }

            string ProductId = catalogResult.Data.Id;
            string ProductName = catalogResult.Data.Name;
            
            var newInventory = _mapper.Map<Inventory>(inventoryDto);
            newInventory.Id = IdGenerator.GenerateId(ClassPrefix.Inventory);
            newInventory.ProductName = ProductName;
            newInventory.ProductId = ProductId;
            newInventory.CreatedAt = DateTime.UtcNow;
            newInventory.WarehouseId = warehouseId;

            existingWarehouse.Inventories.Add(newInventory);
            var saved = await _warehouseUow.Warehouse.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi thêm hàng vào kho.");
            }

            await _cache.RemoveAsync("all_warehouses");
            await _cache.RemoveAsync($"warehouse_{warehouseId}");
            return ApiResponse<bool>.Success(true, "Nhập hàng vào kho thành công.");
        }

        public async Task<ApiResponse<bool>> DirectStockOutAsync(string warehouseId, DirectStockOutDTO stockOutDto)
        {
            var existingWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(warehouseId);
            if (existingWarehouse == null)
            {
                return ApiResponse<bool>.Failure($"Không tìm thấy kho hàng với ID: {warehouseId}", 404);
            }
            var inventoryItem = existingWarehouse.Inventories.FirstOrDefault(i => i.ProductId == stockOutDto.ProductId);
            // Kiểm tra tồn tại sản phẩm trong kho
            if (inventoryItem == null)
                return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với mã: {stockOutDto.ProductId} trong kho hàng.", 404);

            // Tính toán số lượng hàng khả dụng
            int availableQuantity = inventoryItem.Quantity - inventoryItem.ReservedQuantity;

            // Kiểm tra đủ số lượng để xuất
            if (availableQuantity < stockOutDto.Quantity)
            {
                return ApiResponse<bool>.Failure($"Không đủ hàng để xuất! Trong kho chỉ có thể xuất tối đa {availableQuantity} sản phẩm.", 400);
            }
            // Thực hiện xuất hàng
            inventoryItem.Quantity -= stockOutDto.Quantity;
            inventoryItem.UpdatedAt = DateTime.UtcNow;
            // Nếu số lượng sau khi xuất hàng bằng 0, ta sẽ xóa luôn mục tồn kho đó để tránh lưu trữ những mục không còn giá trị
            if (inventoryItem.Quantity == 0)
            {
                existingWarehouse.Inventories.Remove(inventoryItem);
            }
            var saved = await _warehouseUow.Warehouse.SaveChangeAsync();
            if (!saved)
            {
                return ApiResponse<bool>.Failure("Lỗi hệ thống khi xuất hàng từ kho.", 500);
            }

            await _cache.RemoveAsync("all_warehouses");
            await _cache.RemoveAsync($"warehouse_{warehouseId}");
            return ApiResponse<bool>.Success(true, "Xuất hàng từ kho thành công.");
        }

        public async Task<ApiResponse<bool>> TransferInventoryAsync(string fromWarehouseId, TransferInventoryDTO dto)
        {
            if (fromWarehouseId == dto.ToWarehouseId)
                return ApiResponse<bool>.Failure("Kho nguồn và kho đích không được trùng nhau.", 400);

            await _warehouseUow.BeginTransactionAsync();

            try
            {
                var fromWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(fromWarehouseId);
                if (fromWarehouse == null)
                    throw new Exception("Không tìm thấy kho nguồn.");

                var inventoryItemA = fromWarehouse.Inventories.FirstOrDefault(i => i.ProductId == dto.ProductId);
                if (inventoryItemA == null || inventoryItemA.Quantity < dto.Quantity)
                    throw new Exception("Kho nguồn không có sản phẩm này hoặc không đủ số lượng để chuyển.");

                var toWarehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(dto.ToWarehouseId);
                if (toWarehouse == null)
                    throw new Exception("Không tìm thấy kho đích.");

                var currentUsedCapacityB = toWarehouse.Inventories.Sum(i => i.Quantity);
                if (currentUsedCapacityB + dto.Quantity > toWarehouse.Capacity)
                {
                    var remaining = toWarehouse.Capacity - currentUsedCapacityB;
                    throw new Exception($"Kho đích không đủ sức chứa! Chỉ còn trống: {remaining}");
                }

                inventoryItemA.Quantity -= dto.Quantity;
                if (inventoryItemA.Quantity == 0)
                {
                    fromWarehouse.Inventories.Remove(inventoryItemA); 
                }

                var inventoryItemB = toWarehouse.Inventories.FirstOrDefault(i => i.ProductId == dto.ProductId);
                if (inventoryItemB != null)
                {
                    inventoryItemB.Quantity += dto.Quantity;
                }
                else
                {

                    var newInventory = new Inventory
                    {
                        Id = IdGenerator.GenerateId(ClassPrefix.Inventory),
                        ProductId = dto.ProductId,
                        ProductName = inventoryItemA.ProductName, 
                        Quantity = dto.Quantity,
                        WarehouseId = dto.ToWarehouseId,
                        CreatedAt = DateTime.UtcNow
                    };
                    toWarehouse.Inventories.Add(newInventory);
                }

                await _warehouseUow.CommitAsync();
                await _cache.RemoveAsync("all_warehouses");
                await _cache.RemoveAsync($"warehouse_{fromWarehouseId}");
                await _cache.RemoveAsync($"warehouse_{dto.ToWarehouseId}");
                return ApiResponse<bool>.Success(true, "Chuyển kho thành công!");
            }
            catch (Exception ex)
            {
                await _warehouseUow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message);
            }
        }

        public async Task<ApiResponse<bool>> ReserveStockAsync(string warehouseId, ReserveStockDTO reserveStockDto)
        {
            await _warehouseUow.BeginTransactionAsync();
            try
            {
                var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(warehouseId);
                if (warehouse == null) throw new Exception("Không tìm thấy kho hàng.");

                var inventoryItem = warehouse.Inventories.FirstOrDefault(i => i.ProductId == reserveStockDto.ProductId);
                if (inventoryItem == null) throw new Exception("Không tìm thấy sản phẩm trong kho hàng.");

                //Tính số lượng còn CÓ THỂ BÁN
                int availableQuantity = inventoryItem.Quantity - inventoryItem.ReservedQuantity;
                if (availableQuantity < reserveStockDto.Quantity)
                {
                    throw new Exception($"Không đủ hàng khả dụng! Tổng: {inventoryItem.Quantity}, Đang giữ: {inventoryItem.ReservedQuantity}, Có thể bán: {availableQuantity}");
                }

                // Khóa số lượng hàng lại
                inventoryItem.ReservedQuantity += reserveStockDto.Quantity;

                await _warehouseUow.CommitAsync();
                await _cache.RemoveAsync("all_warehouses");
                await _cache.RemoveAsync($"warehouse_{warehouseId}");
                return ApiResponse<bool>.Success(true, "Giữ hàng thành công!");
            }
            catch (Exception ex)
            {
                await _warehouseUow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
        }

        public async Task<ApiResponse<bool>> ReleaseReservedStockAsync(string warehouseId, ReleaseStockDTO releaseStockDto)
        {
            await _warehouseUow.BeginTransactionAsync();
            try
            {
                var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(warehouseId);
                if (warehouse == null) throw new Exception("Không tìm thấy kho hàng.");

                var inventoryItem = warehouse.Inventories.FirstOrDefault(i => i.ProductId == releaseStockDto.ProductId);
                if (inventoryItem == null) throw new Exception("Không tìm thấy sản phẩm trong kho hàng.");

                if (inventoryItem.ReservedQuantity < releaseStockDto.Quantity)
                {
                    throw new Exception($"Số lượng cần giải phóng vượt quá số lượng đang giữ! Đang giữ: {inventoryItem.ReservedQuantity}, Cần giải phóng: {releaseStockDto.Quantity}");
                }

                //Trả lại số lượng đang giữ
                inventoryItem.ReservedQuantity -= releaseStockDto.Quantity;

                await _warehouseUow.CommitAsync();
                await _cache.RemoveAsync("all_warehouses");
                await _cache.RemoveAsync($"warehouse_{warehouseId}");
                return ApiResponse<bool>.Success(true, "Giải phóng hàng đã giữ thành công!");
            }
            catch (Exception ex)
            {
                await _warehouseUow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
        }

        public async Task<ApiResponse<bool>> ConfirmStockOutAsync(string warehouseId, ConfirmStockOutDTO confirmStockOutDto)
        {
            await _warehouseUow.BeginTransactionAsync();
            try
            {
                var warehouse = await _warehouseUow.Warehouse.GetWarehouseWithInventoriesAsync(warehouseId);
                if (warehouse == null) throw new Exception("Không tìm thấy kho hàng.");

                var inventoryItem = warehouse.Inventories.FirstOrDefault(i => i.ProductId == confirmStockOutDto.ProductId);
                if (inventoryItem == null) throw new Exception("Không tìm thấy sản phẩm trong kho hàng.");

                if (inventoryItem.ReservedQuantity < confirmStockOutDto.Quantity)
                {
                    throw new Exception($"Số lượng cần xuất vượt quá số lượng đang giữ! Đang giữ: {inventoryItem.ReservedQuantity}, Cần xuất: {confirmStockOutDto.Quantity}");
                }

                // Thực hiện xuất hàng (Khách đã lấy hàng)
                inventoryItem.Quantity -= confirmStockOutDto.Quantity;
                inventoryItem.ReservedQuantity -= confirmStockOutDto.Quantity;

                if(inventoryItem.Quantity == 0)
                {
                    warehouse.Inventories.Remove(inventoryItem);
                }

                await _warehouseUow.CommitAsync();
                await _cache.RemoveAsync("all_warehouses");
                await _cache.RemoveAsync($"warehouse_{warehouseId}");
                return ApiResponse<bool>.Success(true, "Xác nhận xuất hàng thành công!");
            }
            catch (Exception ex)
            {
                await _warehouseUow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
        }
    }
}
