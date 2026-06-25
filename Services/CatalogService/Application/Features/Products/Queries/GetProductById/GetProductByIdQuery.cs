using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQuery : IRequest<ApiResponse<ProductDTO>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
