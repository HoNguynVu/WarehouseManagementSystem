using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class UpdateProductDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal? Price { get; set; }
        public string? CategoryId { get; set; }
        public string? ImageUrl { get; set; }
        public Dictionary<string, string>? Specifications { get; set; }
    }
}
