using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using Application.Settings;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ZaloPaySettings _zaloConfig;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOrderUow _uow;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IOptions<ZaloPaySettings> zaloConfig,
            IHttpClientFactory httpClientFactory,
            IOrderUow uow,
            ILogger<PaymentService> logger)
        {
            _zaloConfig = zaloConfig.Value;
            _httpClientFactory = httpClientFactory;
            _uow = uow;
            _logger = logger;
        }

        public async Task<(int StatusCode, PaymentLinkDTO dto)> CreateZaloPayLinkForOrder(string orderId, decimal amount)
        {
            if (string.IsNullOrEmpty(orderId))
                return (400, new PaymentLinkDTO { IsSuccess = false, Message = "Invalid order ID" });

            // Generate a unique appTransId to allow retrying payments for the same order
            var appTransId = $"{DateTime.Now:yyMMdd}_{orderId}_{DateTime.Now:HHmmss}";
            string description = $"Thanh toan don hang {orderId}";

            string orderUrl = await CallZaloPayCreateOrder(appTransId, (long)amount, description, orderId);
            if (string.IsNullOrEmpty(orderUrl))
                return (500, new PaymentLinkDTO { IsSuccess = false, Message = "Failed to create ZaloPay order" });

            var existingPayment = await _uow.Payments.GetByOrderIdAsync(orderId);

            await _uow.BeginTransactionAsync();
            if (existingPayment != null)
            {
                existingPayment.TransactionId = appTransId;
                existingPayment.Amount = amount;
                existingPayment.Status = PaymentConstants.StatusPending;
                existingPayment.UpdatedAt = DateTimeOffset.UtcNow;
                _uow.Payments.Update(existingPayment);
            }
            else
            {
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
            }
            await _uow.CommitAsync();

            return (200, new PaymentLinkDTO { IsSuccess = true, PaymentId = appTransId, PaymentUrl = orderUrl, Message = "ZaloPay payment link created successfully." });
        }

        private async Task<string> CallZaloPayCreateOrder(string appTransId, long amount, string description, string orderId)
        {
            try
            {
                var embedData = new { redirecturl = _zaloConfig.FrontEndUrl };
                var items = new[] { new { orderId } };

                // Fix: ZaloPay Sandbox limits transactions to 10M VND.
                long sandboxAmount = amount > 10000000 ? 50000 : amount;

                var param = new Dictionary<string, string>
                {
                    { "app_id", _zaloConfig.AppId },
                    { "app_user", "Warehouse Management System" },
                    { "app_time", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                    { "amount", sandboxAmount.ToString() },
                    { "app_trans_id", appTransId },
                    { "embed_data", JsonConvert.SerializeObject(embedData) },
                    { "item", JsonConvert.SerializeObject(items) },
                    { "description", description },
                    { "bank_code", "VNBANK" }
                };

                if (!string.IsNullOrEmpty(_zaloConfig.CallbackUrl))
                {
                    param.Add("callback_url", _zaloConfig.CallbackUrl);
                }

                var data = $"{param["app_id"]}|{param["app_trans_id"]}|{param["app_user"]}|{param["amount"]}|{param["app_time"]}|{param["embed_data"]}|{param["item"]}";
                param.Add("mac", CryptoHelper.HmacSha256(_zaloConfig.Key1, data));

                var client = _httpClientFactory.CreateClient();
                var content = new FormUrlEncodedContent(param);
                var response = await client.PostAsync(_zaloConfig.CreateOrderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultString = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("ZaloPay Response: {Response}", resultString);
                    var result = JsonConvert.DeserializeObject<dynamic>(resultString);
                    if (result != null && result.return_code == 1)
                    {
                        return result.order_url;
                    }
                }
                else 
                {
                    _logger.LogError("ZaloPay API returned status code: {StatusCode}", response.StatusCode);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ZaloPay API");
                return string.Empty;
            }
        }

    }
}
