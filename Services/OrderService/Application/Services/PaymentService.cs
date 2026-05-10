using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SharedLibrary.IntergrationEvents;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ZaloPaySettings _zaloConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOrderUow _uow;
        private readonly ILogger<PaymentService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentService(
            IOptions<ZaloPaySettings> zaloConfig,
            IHttpClientFactory httpClientFactory,
            IOrderUow uow,
            ILogger<PaymentService> logger,
            IPublishEndpoint publishEndpoint)
        {
            _zaloConfig = zaloConfig.Value;
            _httpClientFactory = httpClientFactory;
            _uow = uow;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<(int StatusCode, PaymentLinkDTO dto)> CreateZaloPayLinkForOrder(string orderId, decimal amount)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return (400, new PaymentLinkDTO { IsSuccess = false, Message = "Invalid order ID" });
            }

            var appTransId = IdGenerator.GenerateId(PaymentConstants.PrefixOrder);
            string description = $"Thanh toan don hang {orderId}";

            string orderUrl = await CallZaloPayCreateOrder(appTransId, (long)amount, description, orderId);
            if (string.IsNullOrEmpty(orderUrl))
            {
                return (500, new PaymentLinkDTO { IsSuccess = false, Message = "Failed to create ZaloPay order" });
            }

            var payment = new Payment
            {
                Id = IdGenerator.GenerateId("PAY"),
                OrderId = orderId,
                PaymentMethod = PaymentConstants.MethodZaloPay,
                Amount = amount,
                TransactionId = appTransId,
                Status = PaymentConstants.StatusPending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _uow.Payments.Create(payment);
            await _uow.CommitAsync();

            return (200, new PaymentLinkDTO { IsSuccess = true, PaymentId = appTransId, PaymentUrl = orderUrl, Message = "ZaloPay payment link created successfully." });
        }

        private async Task<string> CallZaloPayCreateOrder(string appTransId, long amount, string description, string orderId)
        {
            try
            {
                var embedData = new { redirecturl = _zaloConfig.FrontEndUrl };
                var items = new[] { new { orderId } };

                var param = new Dictionary<string, string>
                {
                    { "app_id", _zaloConfig.AppId },
                    { "app_user", "Warehouse Management System" },
                    { "app_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                    { "amount", amount.ToString() },
                    { "app_trans_id", appTransId },
                    { "embed_data", JsonConvert.SerializeObject(embedData) },
                    { "item", JsonConvert.SerializeObject(items) },
                    { "description", description },
                    { "bank_code", "zalopayapp" }
                };

                var data = $"{param["app_id"]}|{param["app_trans_id"]}|{param["app_user"]}|{param["amount"]}|{param["app_time"]}|{param["embed_data"]}|{param["item"]}";
                param.Add("mac", CryptoHelper.HmacSha256(_zaloConfig.Key1, data));

                var client = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(_zaloConfig.CreateOrderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(resultString);
                    if (result != null && result.return_code == 1)
                    {
                        return result.order_url;
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ZaloPay API");
                return string.Empty;
            }
        }

        public async Task<bool> ProcessCallback(ZaloPayCallbackDTO cbdata)
        {
            try
            {
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

                if (payment.Status == PaymentConstants.StatusCompleted)
                {
                    // Already processed
                    return true;
                }

                payment.Status = PaymentConstants.StatusCompleted;
                payment.UpdatedAt = DateTimeOffset.UtcNow;
                _uow.Payments.Update(payment);

                var order = await _uow.Orders.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    order.Status = "Paid";
                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    _uow.Orders.Update(order);
                }

                await _uow.CommitAsync();

                // Publish Event
                await _publishEndpoint.Publish(new PaymentSuccessEvent
                {
                    OrderId = payment.OrderId,
                    TransactionId = payment.TransactionId,
                    Amount = payment.Amount,
                    PaymentDate = DateTime.UtcNow
                });

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
