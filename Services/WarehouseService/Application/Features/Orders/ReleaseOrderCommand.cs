using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders
{
    public class ReleaseOrderCommand : IRequest<ApiResponse<bool>>
    {
        public string OrderId { get; set; } = string.Empty;
    }
}
