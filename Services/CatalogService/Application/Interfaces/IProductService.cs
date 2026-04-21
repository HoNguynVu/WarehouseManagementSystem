using Application.DTOs;
using SharedLibrary.Responses;

namespace Application.Interfaces
{
    public interface IProductService 
    {
        Task<ApiResponse<IEnumerable<ProductDTO>>> GetAllProductsAsync();
        Task<ApiResponse<ProductDTO>> GetProductByIdAsync(string id);
        Task<ApiResponse<ProductDTO>> CreateProductAsync(CreateProductDTO productDto);
        Task<ApiResponse<ProductDTO>> UpdateProductAsync(string id, UpdateProductDTO productDto);
        Task<ApiResponse<bool>> DeleteProductAsync(string id);
    }
}
