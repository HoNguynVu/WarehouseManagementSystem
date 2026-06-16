using Domain.Enums;
using SharedLibrary.Seedwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Order : BaseEntity<string>
    {
        public string AccountId { get; set; } = string.Empty; 
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = OrderStatus.Pending;
        public string PaymentMethod { get; set; } = "ZaloPay";
        
        // Shipping Information
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment Payment { get; set; }
    }
}
