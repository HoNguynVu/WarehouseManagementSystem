using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class AddInventoryDTO
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public string ProductId { get; set; } = string.Empty;
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string ProductName { get; set; } = string.Empty;
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
    }
}
