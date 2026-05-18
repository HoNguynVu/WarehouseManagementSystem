using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Payment : BaseEntity<string>
    {
        public string OrderId { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; } 
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public virtual Order Order { get; set; }
    }
}
