using System.Collections.Generic;

namespace Application.DTOs
{
    public class CreateOrderDTO
    {
        public string PaymentMethod { get; set; } = "ZaloPay";
        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    }

    public class OrderItemDTO
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
