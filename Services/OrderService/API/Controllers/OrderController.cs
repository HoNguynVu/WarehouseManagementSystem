using Application.DTOs;
using Application.Features.Orders.Commands.CreateOrder;
using Application.Features.Orders.Queries.GetAllOrders;
using Application.Features.Orders.Queries.GetOrderById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrderController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDTO dto)
        {
            var accountId = User.FindFirst("accountId")?.Value;
            if (string.IsNullOrEmpty(accountId))
                return Unauthorized();

            var response = await _mediator.Send(new CreateOrderCommand { Dto = dto, AccountId = accountId });
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return CreatedAtAction(nameof(GetById), new { id = response.Data?.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _mediator.Send(new GetOrderByIdQuery { Id = id });
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetAllOrdersQuery());
            if (!response.IsSuccess)
                return StatusCode(response.StatusCode, response);

            return Ok(response);
        }
    }
}
