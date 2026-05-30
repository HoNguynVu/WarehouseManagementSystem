using System;
using SharedLibrary.Seedwork;
using MediatR;

namespace Domain.Events
{
    public class StockReservedEvent : IDomainEvent, INotification
    {
        public string OrderId { get; }
        public string ProductId { get; }
        public string WarehouseId { get; }
        public int Quantity { get; }
        public DateTimeOffset OccurredOn { get; }

        public StockReservedEvent(string orderId, string productId, string warehouseId, int quantity)
        {
            OrderId = orderId;
            ProductId = productId;
            WarehouseId = warehouseId;
            Quantity = quantity;
            OccurredOn = DateTimeOffset.UtcNow;
        }
    }
}
