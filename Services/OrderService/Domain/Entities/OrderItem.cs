using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class OrderItem : BaseEntity<string>
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public virtual Order Order { get; set; }
    }
}
