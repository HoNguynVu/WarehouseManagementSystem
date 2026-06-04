using System.Collections.Generic;

namespace SharedLibrary.IntegrationEvents
{
    public class OrderSubmittedEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<OrderItemMessage> Items { get; set; } = new();
    }

    public class OrderItemMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
