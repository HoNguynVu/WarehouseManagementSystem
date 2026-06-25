using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommand : IRequest<ApiResponse<bool>>
    {
        public string Id { get; set; } = string.Empty;
    }
}
