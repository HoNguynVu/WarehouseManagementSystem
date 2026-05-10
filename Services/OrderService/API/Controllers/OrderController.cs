using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderDTO dto)
        {
            var response = await _orderService.CreateOrderAsync(dto);
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return CreatedAtAction(nameof(GetById), new { id = response.Data?.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _orderService.GetOrderByIdAsync(id);
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _orderService.GetAllOrdersAsync();
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
