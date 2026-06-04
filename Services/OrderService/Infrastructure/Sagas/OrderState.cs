using MassTransit;
using System;

namespace Infrastructure.Sagas
{
    public class OrderState : SagaStateMachineInstance, ISagaVersion
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;

        // Business details
        public string OrderId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        
        // Race condition flags
        public bool IsPaid { get; set; }
        public bool IsStockAllocated { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int Version { get; set; }
    }
}
