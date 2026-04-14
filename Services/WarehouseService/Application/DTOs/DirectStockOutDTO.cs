using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class DirectStockOutDTO
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        public string Reason { get; set; } = "Xuất kho trực tiếp";
    }
}
