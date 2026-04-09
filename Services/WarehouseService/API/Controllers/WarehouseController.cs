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
        public async Task<IActionResult> Create(WarehouseDTO dto)
        {
            var result = await _warehouseService.CreateWarehouseAsync(dto);
            if (!result) return BadRequest("Không thể tạo kho hàng.");

            return Ok("Tạo kho hàng thành công!");
        }
    }
}
