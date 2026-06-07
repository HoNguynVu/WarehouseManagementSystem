using API.Consumers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;
using AppAllocateOrderCommand = Application.Features.Orders.Commands.AllocateOrderCommand;
using AppReleaseOrderCommand = Application.Features.Orders.Commands.ReleaseOrderCommand;

namespace WarehouseService.Tests;

public class ConsumersTests
{
    [Fact]
    public async Task AllocateOrderConsumer_MapsIntegrationCommandToMediatRCommand()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<AppAllocateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Success(true));
        var consumer = new AllocateOrderConsumer(sender.Object, Mock.Of<ILogger<AllocateOrderConsumer>>());
        var context = new Mock<ConsumeContext<SharedLibrary.IntegrationEvents.AllocateOrderCommand>>();
        context.SetupGet(x => x.Message).Returns(new SharedLibrary.IntegrationEvents.AllocateOrderCommand
        {
            OrderId = "ORD001",
            Items = new List<OrderItemMessage> { new() { ProductId = "PRD001", Quantity = 2 } }
        });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        sender.Verify(x => x.Send(
            It.Is<AppAllocateOrderCommand>(c => c.OrderId == "ORD001" && c.Items.Single().RequiredQuantity == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseOrderStockConsumer_MapsIntegrationCommandToMediatRCommand()
    {
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<AppReleaseOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<bool>.Success(true));
        var consumer = new ReleaseOrderStockConsumer(sender.Object, Mock.Of<ILogger<ReleaseOrderStockConsumer>>());
        var context = new Mock<ConsumeContext<ReleaseOrderStockCommand>>();
        context.SetupGet(x => x.Message).Returns(new ReleaseOrderStockCommand { OrderId = "ORD001" });
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        sender.Verify(x => x.Send(
            It.Is<AppReleaseOrderCommand>(c => c.OrderId == "ORD001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
