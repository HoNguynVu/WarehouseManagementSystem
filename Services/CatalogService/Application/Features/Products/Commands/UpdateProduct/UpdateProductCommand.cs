using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommand : IRequest<ApiResponse<ProductDTO>>
    {
        public string Id { get; set; } = string.Empty;
        public UpdateProductDTO ProductDto { get; set; } = new();
    }
}
