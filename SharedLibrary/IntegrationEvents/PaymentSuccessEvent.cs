using System;

namespace SharedLibrary.IntegrationEvents
{
    public class PaymentSuccessEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
