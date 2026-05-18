using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateCategoryDTO
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mô tả danh mục không được để trống")]
        public string Description { get; set; } = string.Empty;
    }
}
