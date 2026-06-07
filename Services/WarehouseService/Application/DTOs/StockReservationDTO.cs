namespace Application.DTOs
{
    public class StockReservationDTO
    {
        public string Id { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty; // Để UI hiển thị tên kho
        public int Quantity { get; set; }
    }
}
