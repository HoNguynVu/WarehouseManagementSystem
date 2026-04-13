using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class TransferInventoryDTO
    {
        [Required(ErrorMessage = "Kho đích (toWarehouseId) không được để trống.")]
        public string ToWarehouseId { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mã sản phẩm (productId) không được để trống.")]
        public string ProductId { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }
    }
}
