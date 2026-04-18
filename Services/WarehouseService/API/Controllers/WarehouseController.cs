using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Infrastructure.Data;
using Application.DTOs;
using MassTransit;
using Application.Events;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        private readonly IPublishEndpoint _publishEndpoint;
        public WarehouseController(IWarehouseService warehouseService, IPublishEndpoint publishEndpoint)
        {
            _warehouseService = warehouseService;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("test-fake-order")]
        public async Task<IActionResult> FakeOrder([FromBody] OrderAllocatedEvent fakeOrder)
        {
            // Vứt bức thư lên RabbitMQ
            await _publishEndpoint.Publish(fakeOrder);
            return Ok("Đã ném sự kiện Đặt hàng lên RabbitMQ! Hãy check cửa sổ Console (Terminal) xem Consumer có bắt được không nhé!");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateWarehouseDTO dto)
        {
            var response = await _warehouseService.CreateWarehouseAsync(dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var response = await _warehouseService.GetAllWarehousesAsync();
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _warehouseService.GetWarehouseByIdAsync(id);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, UpdateWarehouseDTO dto)
        {
            var response = await _warehouseService.UpdateWarehouseAsync(id, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await _warehouseService.DeleteWarehouseAsync(id);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/inventory")]
        public async Task<IActionResult> AddInventory(string warehouseId, [FromBody] AddInventoryDTO dto)
        {
            var response = await _warehouseService.AddInventoryToWarehouseAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/stock-out")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DirectStockOut(string warehouseId, [FromBody] DirectStockOutDTO dto)
        {
            var response = await _warehouseService.DirectStockOutAsync(warehouseId, dto);

            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/transfer")]
        public async Task<IActionResult> TransferInventory(string warehouseId, [FromBody] TransferInventoryDTO dto)
        {
            var response = await _warehouseService.TransferInventoryAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);

        }

        [HttpPost("{warehouseId}/reserve")]
        public async Task<IActionResult> ReserveStock(string warehouseId, [FromBody] ReserveStockDTO dto)
        {
            var response = await _warehouseService.ReserveStockAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/release")]
        public async Task<IActionResult> ReleaseStock(string warehouseId, [FromBody] ReleaseStockDTO dto)
        {
            var response = await _warehouseService.ReleaseReservedStockAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/confirm-out")]
        public async Task<IActionResult> ConfirmStockOut(string warehouseId, [FromBody] ConfirmStockOutDTO dto)
        {
            var response = await _warehouseService.ConfirmStockOutAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

    }
}