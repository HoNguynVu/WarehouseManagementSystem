using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Orders.Commands.RetryPayment
{
    public class RetryPaymentCommandHandler : IRequestHandler<RetryPaymentCommand, ApiResponse<PaymentLinkDTO>>
    {
        private readonly IOrderUow _uow;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<RetryPaymentCommandHandler> _logger;

        public RetryPaymentCommandHandler(
            IOrderUow uow,
            IPaymentService paymentService,
            ILogger<RetryPaymentCommandHandler> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _logger = logger;
        }

        public async Task<ApiResponse<PaymentLinkDTO>> Handle(RetryPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(request.OrderId);
                if (order == null)
                    return ApiResponse<PaymentLinkDTO>.Failure("Order not found", 404);

                if (order.AccountId != request.AccountId)
                    return ApiResponse<PaymentLinkDTO>.Failure("Unauthorized access to this order", 403);

                if (order.Status != OrderStatus.AwaitingPayment && order.Status != OrderStatus.Pending)
                    return ApiResponse<PaymentLinkDTO>.Failure($"Cannot retry payment for order in status: {order.Status}", 400);

                if (order.PaymentMethod != "ZaloPay")
                    return ApiResponse<PaymentLinkDTO>.Failure("Order payment method is not ZaloPay", 400);

                var paymentResult = await _paymentService.CreateZaloPayLinkForOrder(order.Id, order.TotalAmount);
                if (!paymentResult.dto.IsSuccess)
                {
                    return ApiResponse<PaymentLinkDTO>.Failure("Failed to generate new ZaloPay link", paymentResult.StatusCode);
                }

                return ApiResponse<PaymentLinkDTO>.Success(paymentResult.dto, "New payment link generated successfully", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating retry payment link for order {OrderId}", request.OrderId);
                return ApiResponse<PaymentLinkDTO>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
