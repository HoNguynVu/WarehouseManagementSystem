using System.Collections.Generic;

namespace SharedLibrary.IntegrationEvents
{
    public class AllocateOrderCommand
    {
        public string OrderId { get; set; } = string.Empty;
        public List<OrderItemMessage> Items { get; set; } = new();
    }
}
