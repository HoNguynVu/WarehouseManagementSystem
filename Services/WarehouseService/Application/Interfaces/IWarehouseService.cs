using Application.DTOs;

namespace Application.Interfaces
{
    public interface IWarehouseService
    {
        Task<bool> CreateWarehouseAsync(WarehouseDTO warehouseDto);
    }
}
