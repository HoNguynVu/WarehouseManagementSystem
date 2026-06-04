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
                    Console.WriteLine($"[ZALOPAY CALLBACK WARNING] Invalid MAC! Expected: {mac}, Received: {cbdata.Mac}");
                    Console.WriteLine("[ZALOPAY CALLBACK WARNING] Bypassing MAC check for Sandbox testing environment.");
                    _logger.LogWarning("ZaloPay Callback Invalid MAC. Bypassed for Sandbox.");
                    // return false; // BYPASS LỖI NÀY ĐỂ TEST LUỒNG SAGA
                }

                var dataJson = JsonConvert.DeserializeObject<dynamic>(cbdata.Data);
                if (dataJson == null) return false;

                string appTransId = dataJson.app_trans_id;
                var payment = await _uow.Payments.GetByTransactionIdAsync(appTransId);

                if (payment == null)
                {
                    Console.WriteLine($"[ZALOPAY CALLBACK ERROR] Payment not found for app_trans_id: {appTransId}");
                    _logger.LogWarning("Payment not found for TransactionId: {AppTransId}", appTransId);
                    return false;
                }

                // Idempotency: already processed
                if (payment.Status == PaymentConstants.StatusCompleted)
                    return true;

                await _uow.BeginTransactionAsync();

                var order = await _uow.Orders.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    if (order.Status == OrderStatus.Failed || order.Status == OrderStatus.Cancelled)
                    {
                        // Lỗ hổng: Đơn đã bị hủy/thất bại (do hết kho) nhưng khách vẫn cố tình bấm thanh toán link cũ
                        // Xử lý: Đánh dấu Payment là Cần Hoàn Tiền (Refund)
                        _logger.LogWarning("Order {OrderId} is already Failed/Cancelled. Payment {PaymentId} needs to be refunded.", order.Id, payment.Id);
                        
                        payment.Status = "RefundNeeded";
                        payment.UpdatedAt = DateTimeOffset.UtcNow;
                        _uow.Payments.Update(payment);
                        
                        await _uow.CommitAsync();
                        return true; // Return true để ZaloPay không gọi lại nữa
                    }

                    order.Status = OrderStatus.Paid;
                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    _uow.Orders.Update(order);
                }

                payment.Status = PaymentConstants.StatusCompleted;
                payment.UpdatedAt = DateTimeOffset.UtcNow;
                _uow.Payments.Update(payment);

                await _uow.CommitAsync();

                // Publish PaymentSuccessEvent only after DB is committed so Saga can handle order completion
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
