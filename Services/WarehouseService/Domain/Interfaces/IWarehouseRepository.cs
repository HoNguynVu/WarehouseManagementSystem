using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task AddAsync(Warehouse warehouse);
        Task<bool> SaveChangeAsync();
    }
}
