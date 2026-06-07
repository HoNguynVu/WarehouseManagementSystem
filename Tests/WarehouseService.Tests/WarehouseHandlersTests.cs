using Application.DTOs;
using Application.Features.Warehouses.Commands.CreateWarehouse;
using Application.Features.Warehouses.Commands.DeleteWarehouse;
using Application.Features.Warehouses.Commands.UpdateWarehouse;
using Application.Features.Warehouses.Queries.GetWarehouseById;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using SharedLibrary.Exceptions;

namespace WarehouseService.Tests;

public class WarehouseHandlersTests
{
    private readonly Mock<IWarehouseUow> _uow = new();
    private readonly Mock<IWarehouseRepository> _warehouse = new();
    private readonly Mock<IDistributedCache> _cache = new();

    public WarehouseHandlersTests()
    {
        _uow.SetupGet(x => x.Warehouse).Returns(_warehouse.Object);
    }

    [Fact]
    public async Task CreateWarehouse_WhenSaveSucceeds_ReturnsCreatedAndInvalidatesCache()
    {
        _uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new CreateWarehouseCommandHandler(_uow.Object, WarehouseTestHelpers.Mapper, _cache.Object);

        var result = await handler.Handle(new CreateWarehouseCommand
        {
            Name = "Main Warehouse",
            Address = "HCM",
            Capacity = 1000
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data!.Name.Should().Be("Main Warehouse");
        _warehouse.Verify(x => x.AddAsync(It.IsAny<Warehouse>()), Times.Once);
        _cache.Verify(x => x.RemoveAsync("all_warehouses", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWarehouse_WhenMissing_ThrowsNotFound()
    {
        _warehouse.Setup(x => x.GetByIdAsync("WH404")).ReturnsAsync((Warehouse?)null);
        var handler = new DeleteWarehouseCommandHandler(_uow.Object, _cache.Object);

        var act = () => handler.Handle(new DeleteWarehouseCommand("WH404"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateWarehouse_WhenValid_UpdatesAndInvalidatesCache()
    {
        var warehouse = new Warehouse { Id = "WH001", Name = "Old", Address = "Old", Capacity = 100 };
        _warehouse.Setup(x => x.GetByIdAsync("WH001")).ReturnsAsync(warehouse);
        _uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var handler = new UpdateWarehouseCommandHandler(_uow.Object, WarehouseTestHelpers.Mapper, _cache.Object);

        var result = await handler.Handle(new UpdateWarehouseCommand
        {
            Id = "WH001",
            Name = "New",
            Address = "HCM",
            Capacity = 200
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        warehouse.Name.Should().Be("New");
        _warehouse.Verify(x => x.Update(warehouse), Times.Once);
        _cache.Verify(x => x.RemoveAsync("warehouse_WH001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWarehouseById_WhenCacheHit_ReturnsCachedDto()
    {
        _cache.SetupJson("warehouse_WH001", new WarehouseDTO { Id = "WH001", Name = "Cached" });
        var handler = new GetWarehouseByIdQueryHandler(_uow.Object, WarehouseTestHelpers.Mapper, _cache.Object);

        var result = await handler.Handle(new GetWarehouseByIdQuery("WH001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Name.Should().Be("Cached");
        _warehouse.Verify(x => x.GetWarehouseWithInventoriesAsync(It.IsAny<string>()), Times.Never);
    }
}
