using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UpdateProductDTO
    {
        public string? Name { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public double? Price { get; set; }
        public string? CategoryId { get; set; } = string.Empty;
    }
}
