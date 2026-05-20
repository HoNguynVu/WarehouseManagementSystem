using Application.DTOs;
using MediatR;

namespace Application.Features.Payments.Commands.ProcessPaymentCallback
{
    public class ProcessPaymentCallbackCommand : IRequest<bool>
    {
        public ZaloPayCallbackDTO Cbdata { get; set; } = null!;
    }
}
