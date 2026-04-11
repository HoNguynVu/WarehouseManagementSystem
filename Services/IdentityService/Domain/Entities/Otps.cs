using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Otps : BaseEntity<string>
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpirationTime { get; set; }
        public bool IsActive { get; set; }
        public string Purpose { get; set; } = string.Empty; 
        public string AccountId { get; set; } = string.Empty;
        public Accounts Account { get; set; }
    }
}
