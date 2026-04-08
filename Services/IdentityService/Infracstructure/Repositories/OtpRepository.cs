using Domain.Entities;
using Domain.Interfaces;
using Infracstructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infracstructure.Repositories
{
    public class OtpRepository : GenericRepositories<Otps, string>, IOtpRepository
    {
        public OtpRepository(IdentityDbContext context) : base(context)
        {
        }
        public async Task<Otps?> GetByCodeAsync(string code)
        {
            return await _dbSet.Where(o => o.Code.ToString() == code).FirstOrDefaultAsync();
        }
        public Task<Otps?> GetByEmailAsync(string email)
        {
            return _dbSet.Where(o => o.Account.Email == email && o.IsActive).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
        }
    }
}
