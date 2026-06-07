using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Accounts : BaseEntity<string>
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ICollection<Otps> Otps { get; set; } = new List<Otps>();
        public ICollection<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();
        public ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();
    }
}
