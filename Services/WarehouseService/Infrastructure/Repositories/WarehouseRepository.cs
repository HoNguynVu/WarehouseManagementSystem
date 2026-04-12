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
        public async Task<bool> SaveChangeAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<IEnumerable<Warehouse>> GetAllAsync()
        {
            return await _context.Warehouses.ToListAsync();
        }
        public async Task<Warehouse?> GetByIdAsync(string id)
        {
            return await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
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
    }
}
