using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class JwtGenerator 
    {
        private readonly JwtSettings _jwtSettings;
        public JwtGenerator(IOptions<JwtSettings> jwtSettings) //IOptions to hot reload configuration without restarting the application
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateToken(Accounts account)
        {
            var claims = new List<Claim>
            {
                new Claim("accountId", account.Id),
                new Claim("username", account.Username),
                new Claim("email", account.Email),
                new Claim("role", account.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
