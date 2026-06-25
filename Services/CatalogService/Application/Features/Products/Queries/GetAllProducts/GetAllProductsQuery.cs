using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsQuery : IRequest<ApiResponse<IEnumerable<ProductDTO>>>
    {
    }
}
