using Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class AuthHelpers
    {
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public RefreshTokens CreateRefreshToken(string accountId)
        {
            return new RefreshTokens
            {
                Id = ClassPrefix.RefreshToken + IdGenerator.GenerateId(),
                AccountId = accountId,
                Token = GenerateRefreshToken(),
                ExpirationTime = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };
        }

    }
}
