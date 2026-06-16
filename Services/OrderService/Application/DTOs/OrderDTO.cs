using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class OrderDTO
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        
        // Shipping Information
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public List<OrderItemDTO> OrderItems { get; set; } = new List<OrderItemDTO>();
        public PaymentLinkDTO? PaymentInfo { get; set; }
    }
}
