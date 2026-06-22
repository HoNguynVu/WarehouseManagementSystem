using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task AddAsync(Warehouse warehouse);
        Task<IEnumerable<Warehouse>> GetAllAsync();
        Task<Warehouse?> GetByIdAsync(string id);
        void Update(Warehouse warehouse);
        void Delete(Warehouse warehouse);
        Task <Warehouse?> GetWarehouseWithInventoriesAsync(string id);
        Task<IEnumerable<Warehouse>> GetWarehousesContainingProductAsync(string productId);
        Task AddReservationAsync(StockReservation reservation);
        Task<IEnumerable<StockReservation>> GetReservationsByOrderIdAsync(string orderId);
        Task<Inventory?> GetInventoryAsync(string warehouseId, string productId);
        void DeleteReservation(StockReservation reservation);
        Task<Dictionary<string, int>> GetWarehousesStockAsync();
        Task<IEnumerable<Inventory>> GetLowStockAsync(int threshold);
    }
}
