using Application.DTOs;
using SharedLibrary.Responses;

namespace Application.Interfaces
{
    public interface ICategoryService
    {
        Task<ApiResponse<IEnumerable<CategoryDTO>>> GetAllCategoriesAsync();
        Task<ApiResponse<CategoryDTO>> GetCategoryByIdAsync(string id);
        Task<ApiResponse<CategoryDTO>> CreateCategoryAsync(CreateCategoryDTO categoryDto);
        Task<ApiResponse<CategoryDTO>> UpdateCategoryAsync(string id, UpdateCategoryDTO categoryDto);
        Task<ApiResponse<bool>> DeleteCategoryAsync(string id);
    }
}
