using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public class OrderStatus
    {
        public const string Pending = "Pending";
        public const string AwaitingPayment = "AwaitingPayment";
        public const string Paid = "Paid";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
