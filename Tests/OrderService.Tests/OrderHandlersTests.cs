using Application.DTOs;
using Application.Features.Orders.Commands.CancelOrder;
using Application.Features.Orders.Commands.CreateOrder;
using Application.Features.Orders.Queries.GetOrderById;
using Application.Helpers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.UnitOfWorks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.IntegrationEvents;

namespace OrderService.Tests;

public class OrderHandlersTests
{
    private readonly Mock<IOrderUow> _uow = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IOrderItemRepository> _orderItems = new();
    private readonly Mock<IPaymentRepository> _payments = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly Mock<IPaymentService> _paymentService = new();

    public OrderHandlersTests()
    {
        _uow.SetupGet(x => x.Orders).Returns(_orders.Object);
        _uow.SetupGet(x => x.OrderItems).Returns(_orderItems.Object);
        _uow.SetupGet(x => x.Payments).Returns(_payments.Object);
    }

    [Fact]
    public async Task CreateOrder_WhenItemsMissing_ReturnsBadRequest()
    {
        var handler = CreateCreateOrderHandler();

        var result = await handler.Handle(new CreateOrderCommand
        {
            AccountId = "ACC001",
            Dto = new CreateOrderDTO { Items = new List<OrderItemDTO>() }
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_WhenValid_PublishesOrderSubmittedEvent()
    {
        _paymentService.Setup(x => x.CreateZaloPayLinkForOrder(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync((200, new PaymentLinkDTO { IsSuccess = true, PaymentUrl = "https://pay.test" }));
        var handler = CreateCreateOrderHandler();

        var result = await handler.Handle(new CreateOrderCommand
        {
            AccountId = "ACC001",
            Dto = new CreateOrderDTO
            {
                PaymentMethod = PaymentConstants.MethodZaloPay,
                Items = new List<OrderItemDTO>
                {
                    new() { ProductId = "PRD001", ProductName = "Product", Quantity = 2, UnitPrice = 10 }
                }
            }
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.TotalAmount.Should().Be(20);
        _orders.Verify(x => x.Create(It.IsAny<Order>()), Times.Once);
        _orderItems.Verify(x => x.Create(It.IsAny<OrderItem>()), Times.Once);
        _uow.Verify(x => x.CommitAsync(), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<OrderSubmittedEvent>(e => e.AccountId == "ACC001" && e.TotalAmount == 20 && e.Items.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_WhenUnauthorized_ReturnsForbidden()
    {
        _orders.Setup(x => x.GetByIdAsync("ORD001")).ReturnsAsync(new Order { Id = "ORD001", AccountId = "OWNER" });
        var handler = new CancelOrderCommandHandler(_uow.Object, _publishEndpoint.Object, Mock.Of<ILogger<CancelOrderCommandHandler>>());

        var result = await handler.Handle(new CancelOrderCommand { OrderId = "ORD001", AccountId = "OTHER" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        _publishEndpoint.Verify(x => x.Publish(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WhenValid_PublishesOrderCancelledEvent()
    {
        _orders.Setup(x => x.GetByIdAsync("ORD001"))
            .ReturnsAsync(new Order { Id = "ORD001", AccountId = "ACC001", Status = OrderStatus.AwaitingPayment });
        var handler = new CancelOrderCommandHandler(_uow.Object, _publishEndpoint.Object, Mock.Of<ILogger<CancelOrderCommandHandler>>());

        var result = await handler.Handle(new CancelOrderCommand { OrderId = "ORD001", AccountId = "ACC001" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<OrderCancelledEvent>(e => e.OrderId == "ORD001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderById_WhenMissing_ReturnsNotFound()
    {
        _orders.Setup(x => x.GetByIdAsync("ORD404")).ReturnsAsync((Order?)null);
        var handler = new GetOrderByIdQueryHandler(_uow.Object, OrderTestHelpers.Mapper, Mock.Of<ILogger<GetOrderByIdQueryHandler>>());

        var result = await handler.Handle(new GetOrderByIdQuery { Id = "ORD404" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    private CreateOrderCommandHandler CreateCreateOrderHandler()
    {
        return new CreateOrderCommandHandler(
            _uow.Object,
            _paymentService.Object,
            OrderTestHelpers.Mapper,
            _publishEndpoint.Object,
            Mock.Of<ILogger<CreateOrderCommandHandler>>());
    }
}
