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
    public static class AuthHelpers
    {
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public static RefreshTokens CreateRefreshToken(string accountId)
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

        public static string GenerateOTP()
        {
            var randomNumber = new byte[6];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            int otpValue = BitConverter.ToInt32(randomNumber, 0) % 1000000;
            otpValue = Math.Abs(otpValue);
            return otpValue.ToString("D6");
        }
    }
}
