using SharedLibrary.Seedwork;

namespace Domain.Entities
{
    public class StockReservation : BaseEntity<string>
    {
        // Bảng "Biên lai giữ hàng" - Sinh ra mỗi khi có 1 đơn hàng được tách thành công
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string WarehouseId { get; set; } = string.Empty;
        public int Quantity { get; set; }

        // (Tùy chọn) Điều hướng tới Warehouse để dễ query nếu cần
        public virtual Warehouse Warehouse { get; set; }
    }
}
