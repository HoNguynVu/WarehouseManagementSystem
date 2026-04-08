using Domain.Entities;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IRefreshTokenRepository : IGenericInterface<RefreshTokens, string>
    {
        Task<RefreshTokens?> GetByTokenAsync(string token);
        Task<IEnumerable<RefreshTokens>> GetByAccountIdAsync(string accountId);
        void RevokeTokenAsync(string accountId);
        bool IsTokenValid(string token);
    }
}
