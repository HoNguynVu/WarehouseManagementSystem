using System;

namespace Application.DTOs
{
    public class OrderStateDTO
    {
        public string OrderId { get; set; } = string.Empty;
        public string CurrentState { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public bool IsStockAllocated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
