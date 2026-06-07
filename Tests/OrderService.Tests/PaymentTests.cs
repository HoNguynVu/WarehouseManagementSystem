using System.Net;
using System.Text;
using Application.DTOs;
using Application.Features.Payments.Commands.ProcessPaymentCallback;
using Application.Helpers;
using Application.Services;
using Application.Settings;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedLibrary.IntegrationEvents;

namespace OrderService.Tests;

public class PaymentTests
{
    private readonly Mock<IOrderUow> _uow = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IOrderItemRepository> _items = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();

    public PaymentTests()
    {
        _uow.SetupGet(x => x.Orders).Returns(_orders.Object);
        _uow.SetupGet(x => x.Payments).Returns(_payments.Object);
        _uow.SetupGet(x => x.OrderItems).Returns(_items.Object);
    }

    [Fact]
    public async Task ProcessPaymentCallback_WhenPaymentMissing_ReturnsFalse()
    {
        _payments.Setup(x => x.GetByTransactionIdAsync("250101_ORD001")).ReturnsAsync((Payment?)null);
        var handler = CreateCallbackHandler();

        var result = await handler.Handle(new ProcessPaymentCallbackCommand
        {
            Cbdata = new ZaloPayCallbackDTO { Data = "{\"app_trans_id\":\"250101_ORD001\"}", Mac = "invalid" }
        }, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessPaymentCallback_WhenSuccessful_UpdatesPaymentAndPublishesEvent()
    {
        var payment = new Payment { Id = "PAY001", OrderId = "ORD001", TransactionId = "250101_ORD001", Amount = 99, Status = PaymentConstants.StatusPending };
        var order = new Order { Id = "ORD001", AccountId = "ACC001", Status = OrderStatus.AwaitingPayment };
        _payments.Setup(x => x.GetByTransactionIdAsync("250101_ORD001")).ReturnsAsync(payment);
        _orders.Setup(x => x.GetByIdAsync("ORD001")).ReturnsAsync(order);
        var handler = CreateCallbackHandler();

        var result = await handler.Handle(new ProcessPaymentCallbackCommand
        {
            Cbdata = new ZaloPayCallbackDTO { Data = "{\"app_trans_id\":\"250101_ORD001\"}", Mac = "invalid" }
        }, CancellationToken.None);

        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        payment.Status.Should().Be(PaymentConstants.StatusCompleted);
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<PaymentSuccessEvent>(e => e.OrderId == "ORD001" && e.TransactionId == "250101_ORD001" && e.Amount == 99),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PaymentService_WhenZaloPayReturnsSuccess_CreatesPayment()
    {
        var service = CreatePaymentService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"return_code\":1,\"order_url\":\"https://pay.test/order\"}", Encoding.UTF8, "application/json")
        });

        var result = await service.CreateZaloPayLinkForOrder("ORD001", 100);

        result.StatusCode.Should().Be(200);
        result.dto.IsSuccess.Should().BeTrue();
        result.dto.PaymentUrl.Should().Be("https://pay.test/order");
        _payments.Verify(x => x.Create(It.Is<Payment>(p => p.OrderId == "ORD001" && p.Amount == 100)), Times.Once);
        _uow.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task PaymentService_WhenZaloPayReturnsFailure_DoesNotCreatePayment()
    {
        var service = CreatePaymentService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"return_code\":2}", Encoding.UTF8, "application/json")
        });

        var result = await service.CreateZaloPayLinkForOrder("ORD001", 100);

        result.StatusCode.Should().Be(500);
        result.dto.IsSuccess.Should().BeFalse();
        _payments.Verify(x => x.Create(It.IsAny<Payment>()), Times.Never);
    }

    private ProcessPaymentCallbackCommandHandler CreateCallbackHandler()
    {
        return new ProcessPaymentCallbackCommandHandler(
            _uow.Object,
            _publishEndpoint.Object,
            Options.Create(new ZaloPaySettings { Key2 = "secret" }),
            Mock.Of<ILogger<ProcessPaymentCallbackCommandHandler>>());
    }

    private PaymentService CreatePaymentService(HttpResponseMessage response)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(response));
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new PaymentService(
            Options.Create(new ZaloPaySettings
            {
                AppId = "2553",
                Key1 = "key1",
                CallbackUrl = "https://callback.test",
                CreateOrderUrl = "https://zalopay.test/create",
                FrontEndUrl = "https://frontend.test"
            }),
            httpClientFactory.Object,
            _uow.Object,
            Mock.Of<ILogger<PaymentService>>());
    }
}
