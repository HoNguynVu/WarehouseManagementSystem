using System.Data;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Data
{
    public class WarehouseUow : IWarehouseUow
    {
        private readonly WarehouseDbContext _context;

        //Khai báo property
        public IWarehouseRepository Warehouse { get; }
        public WarehouseUow(WarehouseDbContext context)
        {
            _context = context;
            Warehouse = new WarehouseRepository(_context);
        }
        public async Task<bool> SaveChangeAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public void ClearTracker()
        {
            _context.ChangeTracker.Clear();
        }
    }
}
