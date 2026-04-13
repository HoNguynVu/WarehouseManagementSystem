using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Infrastructure.Data;
using Application.DTOs;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateWarehouseDTO dto)
        {
            var response = await _warehouseService.CreateWarehouseAsync(dto);
            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _warehouseService.GetAllWarehousesAsync();
            if (!response.IsSuccess)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _warehouseService.GetWarehouseByIdAsync(id);
            if (!response.IsSuccess)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, UpdateWarehouseDTO dto)
        {
            var response = await _warehouseService.UpdateWarehouseAsync(id, dto);
            if (!response.IsSuccess)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await _warehouseService.DeleteWarehouseAsync(id);
            if (!response.IsSuccess)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/inventory")]
        public async Task<IActionResult> AddInventory(string warehouseId, [FromBody] AddInventoryDTO dto)
        {
            var response = await _warehouseService.AddInventoryToWarehouseAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/stock-out")]
        public async Task<IActionResult> StockOut(string warehouseId, [FromBody] StockOutDTO dto)
        {
            var response = await _warehouseService.StockOutAsync(warehouseId, dto);

            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("{warehouseId}/transfer")]
        public async Task<IActionResult> TransferInventory(string warehouseId, [FromBody] TransferInventoryDTO dto)
        {
            var response = await _warehouseService.TransferInventoryAsync(warehouseId, dto);
            if (!response.IsSuccess)
            {
                return BadRequest(response);
            }
            return Ok(response);

        }
    }
}