using Application.DTOs;
using MediatR;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Commands.RetryPayment
{
    public class RetryPaymentCommand : IRequest<ApiResponse<PaymentLinkDTO>>
    {
        public string OrderId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
    }
}
