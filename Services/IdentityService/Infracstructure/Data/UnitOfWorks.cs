using Domain.Interfaces;
using Infracstructure.Repositories;
using Infracstructure.UnitOfWorks;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Infracstructure.Data
{
    public class UnitOfWorks : IAuthUow
    {
        private readonly IdentityDbContext _context;
        private IDbContextTransaction? _transaction;

        public IAccountRepository Accounts { get; }
        public IOtpRepository Otps { get; }
        public IRefreshTokenRepository RefreshTokens { get; }

        public UnitOfWorks(IdentityDbContext context, IDbContextTransaction? transaction)
        {
            _context = context;
            _transaction = transaction;

            Accounts = new AccountRepository(_context);
            Otps = new OtpRepository(_context);
            RefreshTokens = new RefreshTokenRepository(_context);
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
