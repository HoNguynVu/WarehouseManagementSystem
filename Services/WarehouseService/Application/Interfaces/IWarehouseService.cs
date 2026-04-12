using Application.DTOs;
using SharedLibrary.Responses;

namespace Application.Interfaces
{
    public interface IWarehouseService
    {
        Task<ApiResponse<Domain.Entities.Warehouse>> CreateWarehouseAsync(CreateWarehouseDTO warehouseDto);
        Task<ApiResponse<IEnumerable<WarehouseDTO>>> GetAllWarehousesAsync();
        Task<ApiResponse<WarehouseDTO>> GetWarehouseByIdAsync(string id);
        Task<ApiResponse<WarehouseDTO>> UpdateWarehouseAsync(string id, UpdateWarehouseDTO warehouseDto);
        Task<ApiResponse<bool>> DeleteWarehouseAsync(string id);
        Task<ApiResponse<bool>> AddInventoryToWarehouseAsync(string warehouseId, AddInventoryDTO inventoryDto);
        Task<ApiResponse<bool>> StockOutAsync(string warehouseId, StockOutDTO stockOutDto);
    }
}
