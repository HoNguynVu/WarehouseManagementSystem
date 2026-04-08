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
    public class RefreshTokenRepository : GenericRepositories<RefreshTokens, string>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(IdentityDbContext context) : base(context)
        {
        }
        public async Task<RefreshTokens?> GetByTokenAsync(string token)
        {
            return await _context.Set<RefreshTokens>().FirstOrDefaultAsync(rt => rt.Token == token);
        }
        public async Task<IEnumerable<RefreshTokens>> GetByAccountIdAsync(string accountId)
        {
            return await _context.Set<RefreshTokens>().Where(rt => rt.AccountId == accountId).ToListAsync();
        }
        public void RevokeTokenAsync(string accountId)
        {
            var tokens = _context.Set<RefreshTokens>().Where(rt => rt.AccountId == accountId).ToList();
            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    token.IsActive = false;
                    token.RevokedAt = DateTime.UtcNow;
                }
            }
        }
        public bool IsTokenValid(string token)
        {
            var refreshToken = _context.Set<RefreshTokens>().FirstOrDefault(rt => rt.Token == token);
            return refreshToken != null && refreshToken.IsActive && refreshToken.ExpirationTime > DateTime.UtcNow;
        }
    }
}
