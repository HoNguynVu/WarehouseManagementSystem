using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task AddAsync(Warehouse warehouse);
        Task<bool> SaveChangeAsync();
        Task<IEnumerable<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(string id);
    }
}
