using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ApiResponse<bool>>
    {
        private readonly IOrderUow _uow;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CancelOrderCommandHandler> _logger;

        public CancelOrderCommandHandler(
            IOrderUow uow,
            IPublishEndpoint publishEndpoint,
            ILogger<CancelOrderCommandHandler> logger)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(request.OrderId);
                if (order == null)
                    return ApiResponse<bool>.Failure("Order not found", 404);

                // Verification of owner
                if (order.AccountId != request.AccountId)
                    return ApiResponse<bool>.Failure("Unauthorized to cancel this order", 403);

                // Cannot cancel completed, already cancelled, or failed orders
                if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Failed)
                    return ApiResponse<bool>.Failure($"Cannot cancel order in status: {order.Status}", 400);

                // Publish OrderCancelledEvent so the Saga state machine starts compensation (Release stock, update db)
                await _publishEndpoint.Publish(new OrderCancelledEvent
                {
                    OrderId = order.Id
                }, cancellationToken);

                _logger.LogInformation("OrderCancelledEvent published for Order {OrderId}", order.Id);

                return ApiResponse<bool>.Success(true, "Order cancellation initiated.", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating order cancellation");
                return ApiResponse<bool>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
