using Application.Helpers;
using Infrastructure.UnitOfWorks;
using MassTransit;
using MediatR;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;

namespace Application.Features.Payments.Commands.MockPayment
{
    public class MockPaymentCommand : IRequest<ApiResponse<bool>>
    {
        public string OrderId { get; set; } = string.Empty;
    }

    public class MockPaymentCommandHandler : IRequestHandler<MockPaymentCommand, ApiResponse<bool>>
    {
        private readonly IOrderUow _uow;
        private readonly IPublishEndpoint _publishEndpoint;

        public MockPaymentCommandHandler(IOrderUow uow, IPublishEndpoint publishEndpoint)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(MockPaymentCommand request, CancellationToken cancellationToken)
        {
            var payment = await _uow.Payments.GetByOrderIdAsync(request.OrderId);
            if (payment == null) return new ApiResponse<bool> { IsSuccess = false, StatusCode = 404, Message = "Payment not found" };

            await _uow.BeginTransactionAsync();

            payment.Status = PaymentConstants.StatusCompleted;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            _uow.Payments.Update(payment);
            await _uow.CommitAsync();

            await _publishEndpoint.Publish(new PaymentSuccessEvent
            {
                OrderId = payment.OrderId,
                TransactionId = payment.TransactionId,
                Amount = payment.Amount,
                PaymentDate = DateTime.UtcNow
            }, cancellationToken);

            return new ApiResponse<bool> { IsSuccess = true, Data = true, StatusCode = 200, Message = "Mock payment successful" };
        }
    }
}
