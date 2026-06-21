using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MediatR;
using SharedLibrary.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

using MassTransit;

namespace Application.Features.Orders.Commands.UpdateOrderStatus
{
    public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResponse<bool>>
    {
        private readonly IOrderUow _uow;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateOrderStatusCommandHandler(IOrderUow uow, IPublishEndpoint publishEndpoint)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(request.OrderId);
                if (order == null)
                    return ApiResponse<bool>.Failure("Order not found", 404);

                await _uow.BeginTransactionAsync();

                if (request.NewStatus == Domain.Enums.OrderStatus.Cancelled && order.Status != Domain.Enums.OrderStatus.Cancelled)
                {
                    await _publishEndpoint.Publish(new SharedLibrary.IntegrationEvents.OrderCancelledEvent
                    {
                        OrderId = order.Id
                    }, cancellationToken);
                }

                order.Status = request.NewStatus;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                _uow.Orders.Update(order);
                await _uow.CommitAsync();

                return ApiResponse<bool>.Success(true, "Order status updated successfully.", 200);
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
