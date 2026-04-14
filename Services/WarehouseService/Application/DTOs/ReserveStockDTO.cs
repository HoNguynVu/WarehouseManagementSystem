using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ReserveStockDTO
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
        public string ProductId { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }
        [Required(ErrorMessage = "Mã đơn hàng không được để trống.")]
        public string OrderId { get; set; } = string.Empty;
    }
}
