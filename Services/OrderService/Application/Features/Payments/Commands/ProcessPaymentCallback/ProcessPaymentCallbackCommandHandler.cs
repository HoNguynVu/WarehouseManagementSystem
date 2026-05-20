using Application.Helpers;
using Application.Settings;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SharedLibrary.IntegrationEvents;

namespace Application.Features.Payments.Commands.ProcessPaymentCallback
{
    public class ProcessPaymentCallbackCommandHandler : IRequestHandler<ProcessPaymentCallbackCommand, bool>
    {
        private readonly IOrderUow _uow;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ZaloPaySettings _zaloConfig;
        private readonly ILogger<ProcessPaymentCallbackCommandHandler> _logger;

        public ProcessPaymentCallbackCommandHandler(
            IOrderUow uow,
            IPublishEndpoint publishEndpoint,
            IOptions<ZaloPaySettings> zaloConfig,
            ILogger<ProcessPaymentCallbackCommandHandler> logger)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
            _zaloConfig = zaloConfig.Value;
            _logger = logger;
        }

        public async Task<bool> Handle(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cbdata = request.Cbdata;
                var mac = CryptoHelper.HmacSha256(_zaloConfig.Key2, cbdata.Data);

                if (mac != cbdata.Mac)
                {
                    _logger.LogWarning("ZaloPay Callback Invalid MAC");
                    return false;
                }

                var dataJson = JsonConvert.DeserializeObject<dynamic>(cbdata.Data);
                if (dataJson == null) return false;

                string appTransId = dataJson.app_trans_id;
                var payment = await _uow.Payments.GetByTransactionIdAsync(appTransId);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for TransactionId: {AppTransId}", appTransId);
                    return false;
                }

                // Idempotency: already processed
                if (payment.Status == PaymentConstants.StatusCompleted)
                    return true;

                await _uow.BeginTransactionAsync();

                payment.Status = PaymentConstants.StatusCompleted;
                payment.UpdatedAt = DateTimeOffset.UtcNow;
                _uow.Payments.Update(payment);

                var order = await _uow.Orders.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    order.Status = OrderStatus.Paid;
                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    _uow.Orders.Update(order);
                }

                await _uow.CommitAsync();

                // Publish events only after DB is committed
                var orderItems = await _uow.OrderItems.GetByOrderId(payment.OrderId);
                await _publishEndpoint.Publish(new CreateOrderEvent
                {
                    OrderId = payment.OrderId,
                    AccountId = order?.AccountId ?? string.Empty,
                    ItemIds = orderItems.Select(i => i.ProductId).ToList()
                }, cancellationToken);

                await _publishEndpoint.Publish(new PaymentSuccessEvent
                {
                    OrderId = payment.OrderId,
                    TransactionId = payment.TransactionId,
                    Amount = payment.Amount,
                    PaymentDate = DateTime.UtcNow
                }, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ZaloPay callback");
                return false;
            }
        }
    }
}
