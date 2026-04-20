using Application.DTOs;
using SharedLibrary.Responses;

namespace Application.Interfaces
{
    public interface ICatalogService 
    {
        Task<ApiResponse<IEnumerable<ProductDTO>>> GetAllProductsAsync();
        Task<ApiResponse<ProductDTO>> GetProductByIdAsync(string id);
        Task<ApiResponse<ProductDTO>> CreateProductAsync(CreateProductDTO productDto);
    }
}
