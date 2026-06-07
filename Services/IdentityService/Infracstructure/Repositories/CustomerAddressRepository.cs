using Domain.Entities;
using Domain.Interfaces;
using Infracstructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infracstructure.Repositories
{
    public class CustomerAddressRepository : GenericRepositories<CustomerAddress, string>, ICustomerAddressRepository
    {
        public CustomerAddressRepository(IdentityDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CustomerAddress>> GetByAccountIdAsync(string accountId)
        {
            return await _dbSet
                .Where(a => a.AccountId == accountId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<CustomerAddress?> GetByIdForAccountAsync(string id, string accountId)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Id == id && a.AccountId == accountId);
        }

        public async Task<bool> AnyForAccountAsync(string accountId)
        {
            return await _dbSet.AnyAsync(a => a.AccountId == accountId);
        }
    }
}
