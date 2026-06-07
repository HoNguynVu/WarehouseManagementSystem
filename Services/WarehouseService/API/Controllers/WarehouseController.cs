using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Application.DTOs;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Application.Features.Orders.Commands;
using Application.Features.Orders.Queries.GetOrderReservations;
using Application.Features.Warehouses.Commands.CreateWarehouse;
using Application.Features.Warehouses.Commands.UpdateWarehouse;
using Application.Features.Warehouses.Commands.DeleteWarehouse;
using Application.Features.Warehouses.Queries.GetAllWarehouses;
using Application.Features.Warehouses.Queries.GetWarehouseById;
using Application.Features.Inventories.Commands.AddInventory;
using Application.Features.Inventories.Commands.DirectStockOut;
using Application.Features.Inventories.Commands.TransferInventory;
using Application.Features.Inventories.Commands.ConfirmStockOut;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMediator _mediator;
        public WarehouseController(IPublishEndpoint publishEndpoint, IMediator mediator)
        {
            _publishEndpoint = publishEndpoint;
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _mediator.Send(new GetAllWarehousesQuery()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            return Ok(await _mediator.Send(new GetWarehouseByIdQuery(id)));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateWarehouseCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            return Ok(await _mediator.Send(new DeleteWarehouseCommand(id)));
        }

        [HttpPost("{warehouseId}/inventory")]
        public async Task<IActionResult> AddInventory(string warehouseId, [FromBody] AddInventoryCommand command)
        {
            command.WarehouseId = warehouseId;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{warehouseId}/stock-out")]
        public async Task<IActionResult> DirectStockOut(string warehouseId, [FromBody] DirectStockOutCommand command)
        {
            command.WarehouseId = warehouseId;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{warehouseId}/transfer")]
        public async Task<IActionResult> TransferInventory(string warehouseId, [FromBody] TransferInventoryCommand command)
        {
            command.FromWarehouseId = warehouseId;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{warehouseId}/confirm-out")]
        public async Task<IActionResult> ConfirmStockOut(string warehouseId, [FromBody] ConfirmStockOutCommand command)
        {
            command.WarehouseId = warehouseId;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("allocate-order")]
        public async Task<IActionResult> AllocateOrder([FromBody] AllocateOrderCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpGet("reservations/order/{orderId}")]
        public async Task<IActionResult> GetOrderReservations(string orderId)
        {
            return Ok(await _mediator.Send(new GetOrderReservationsQuery(orderId)));
        }

        [HttpPost("release-order")]
        public async Task<IActionResult> ReleaseOrder([FromBody] ReleaseOrderCommand command, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(command, cancellationToken));
        }
    }
}