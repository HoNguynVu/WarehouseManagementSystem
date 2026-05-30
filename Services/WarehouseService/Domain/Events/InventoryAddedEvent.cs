using System;
using SharedLibrary.Seedwork;
using MediatR;

namespace Domain.Events
{
    public class InventoryAddedEvent : IDomainEvent, INotification
    {
        public string ProductId { get; }
        public string WarehouseId { get; }
        public int Quantity { get; }
        public DateTimeOffset OccurredOn { get; }

        public InventoryAddedEvent(string productId, string warehouseId, int quantity)
        {
            ProductId = productId;
            WarehouseId = warehouseId;
            Quantity = quantity;
            OccurredOn = DateTimeOffset.UtcNow;
        }
    }
}
