using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO dto)
        {
            //User là property có sẵn của ControllerBase — class mà OrderController kế thừa.
            // Khi request đến, ASP.NET Core middleware JWT Bearer tự động:
            // Đọc header Authorization: Bearer <token>
            // Giải mã JWT token
            // Chuyển tất cả claims trong token thành một ClaimsPrincipal
            // Gán vào HttpContext.User
            var accountId = User.FindFirst("accountId")?.Value; 
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            var response = await _orderService.CreateOrderAsync(dto, accountId);
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
