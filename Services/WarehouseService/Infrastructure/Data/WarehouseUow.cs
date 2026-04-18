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

        private IDbContextTransaction? _transaction;

        //Khai báo property
        public IWarehouseRepository Warehouse { get; }
        public WarehouseUow(WarehouseDbContext context)
        {
            _context = context;
            Warehouse = new WarehouseRepository(_context);
        }
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
            }
        }
        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
            }
        }
        public async Task CommitAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress.");
            }

            try
            {
                await _context.SaveChangesAsync();
                await _transaction.CommitAsync();
            }
            catch
            {
                await _transaction.RollbackAsync();
                throw;
            }
        }
        public async Task RollbackAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress.");
            }
            await _transaction.RollbackAsync();
        }
    }
}
