using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommand : IRequest<ApiResponse<ProductDTO>>
    {
        public CreateProductDTO ProductDto { get; set; } = new();
    }
}
