using Application.Features.Orders.Commands;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using MassTransit;
using Moq;
using SharedLibrary.IntegrationEvents;
using AppAllocateOrderCommand = Application.Features.Orders.Commands.AllocateOrderCommand;

namespace WarehouseService.Tests;

public class OrderStockHandlersTests
{
    private readonly Mock<IWarehouseUow> _uow = new();
    private readonly Mock<IWarehouseRepository> _warehouse = new();
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();

    public OrderStockHandlersTests()
    {
        _uow.SetupGet(x => x.Warehouse).Returns(_warehouse.Object);
    }

    [Fact]
    public async Task AllocateOrder_WhenEnoughStock_ReservesStockAndPublishesAllocated()
    {
        var inventory = new Inventory { ProductId = "PRD001", WarehouseId = "WH001", Quantity = 10, ReservedQuantity = 1 };
        _warehouse.Setup(x => x.GetWarehousesContainingProductAsync("PRD001"))
            .ReturnsAsync(new[] { new Warehouse { Id = "WH001", Inventories = new List<Inventory> { inventory } } });
        _uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new AllocateOrderCommandHandler(_uow.Object, _publishEndpoint.Object);

        var result = await handler.Handle(new AppAllocateOrderCommand
        {
            OrderId = "ORD001",
            Items = new List<OrderItemDto> { new() { ProductId = "PRD001", RequiredQuantity = 3 } }
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        inventory.ReservedQuantity.Should().Be(4);
        _warehouse.Verify(x => x.AddReservationAsync(It.Is<StockReservation>(r => r.OrderId == "ORD001" && r.Quantity == 3)), Times.Once);
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<InventoryAllocatedEvent>(e => e.OrderId == "ORD001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AllocateOrder_WhenStockInsufficient_PublishesAllocationFailed()
    {
        _warehouse.Setup(x => x.GetWarehousesContainingProductAsync("PRD001")).ReturnsAsync(Array.Empty<Warehouse>());
        var handler = new AllocateOrderCommandHandler(_uow.Object, _publishEndpoint.Object);

        var result = await handler.Handle(new AppAllocateOrderCommand
        {
            OrderId = "ORD001",
            Items = new List<OrderItemDto> { new() { ProductId = "PRD001", RequiredQuantity = 3 } }
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        _publishEndpoint.Verify(x => x.Publish(
            It.Is<InventoryAllocationFailedEvent>(e => e.OrderId == "ORD001"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.ClearTracker(), Times.Once);
    }

    [Fact]
    public async Task ReleaseOrder_WhenReservationsExist_ReducesReservedQuantityAndDeletesReservations()
    {
        var reservation = new StockReservation { Id = "RES001", OrderId = "ORD001", ProductId = "PRD001", WarehouseId = "WH001", Quantity = 2 };
        var inventory = new Inventory { ProductId = "PRD001", WarehouseId = "WH001", Quantity = 10, ReservedQuantity = 5 };
        _warehouse.Setup(x => x.GetReservationsByOrderIdAsync("ORD001")).ReturnsAsync(new[] { reservation });
        _warehouse.Setup(x => x.GetInventoryAsync("WH001", "PRD001")).ReturnsAsync(inventory);
        _uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new ReleaseOrderCommandHandler(_uow.Object);

        var result = await handler.Handle(new ReleaseOrderCommand { OrderId = "ORD001" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        inventory.ReservedQuantity.Should().Be(3);
        _warehouse.Verify(x => x.DeleteReservation(reservation), Times.Once);
    }
}
