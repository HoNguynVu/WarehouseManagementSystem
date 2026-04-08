using Domain.Entities;
using Domain.Interfaces;
using Infracstructure.Data;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infracstructure.Repositories
{
    public class AccountRepository : GenericRepositories<Accounts, string>, IAccountRepository
    {
        public AccountRepository(IdentityDbContext context) : base(context)
        {
        }

        public async Task<Accounts?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Accounts?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.Username == username);
        }

        public async Task<IEnumerable<Accounts>> GetByStatusAsync(string status)
        {
            return await _dbSet.Where(a => a.Status == status).ToListAsync();
        }
    } 
}
