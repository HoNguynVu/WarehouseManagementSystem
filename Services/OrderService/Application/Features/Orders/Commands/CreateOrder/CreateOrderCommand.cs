using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommand : IRequest<ApiResponse<OrderDTO>>
    {
        public CreateOrderDTO Dto { get; set; } = null!;
        public string AccountId { get; set; } = string.Empty;
    }
}
