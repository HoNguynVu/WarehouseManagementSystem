using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly WarehouseDbContext _context;
        public WarehouseRepository(WarehouseDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
        }
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses.ToListAsync();
        }
        public async Task<Warehouse?> GetByIdAsync(string id)
        {
            return await _context.Warehouses.Include(w => w.Inventories).FirstOrDefaultAsync(w => w.Id == id);
        }
        public void Update(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
        }
        public void Delete(Warehouse warehouse)
        {
            _context.Warehouses.Remove(warehouse);
        }
        public async Task<Warehouse?> GetWarehouseWithInventoriesAsync(string id)
        {
            return await _context.Warehouses
                .Include(w => w.Inventories)
                .FirstOrDefaultAsync(w => w.Id == id);
        }
        public async Task<IEnumerable<Warehouse>> GetWarehousesContainingProductAsync(string productId)
        {
            return await _context.Warehouses
                .Include(w => w.Inventories)
                .Where(w => w.Inventories.Any(i => i.ProductId == productId))
                .ToListAsync();
        }
        public async Task AddReservationAsync(StockReservation reservation)
        {
            await _context.StockReservations.AddAsync(reservation);
        }
        public async Task<IEnumerable<StockReservation>> GetReservationsByOrderIdAsync(string orderId)
        {
            return await _context.StockReservations
                .Where(r => r.OrderId == orderId)
                .ToListAsync();
        }
        public async Task<Inventory?> GetInventoryAsync(string warehouseId, string productId)
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId);
        }
        public void DeleteReservation(StockReservation reservation)
        {
            _context.StockReservations.Remove(reservation);
        }
        public async Task<Dictionary<string, int>> GetWarehousesStockAsync()
        {
            var stocks = await _context.Inventories
                .GroupBy(i => i.WarehouseId)
                .Select(g => new { WarehouseId = g.Key, CurrentStock = g.Sum(i => i.Quantity) })
                .ToListAsync();
            return stocks.ToDictionary(x => x.WarehouseId, x => x.CurrentStock);
        }
    }
}
