using Application.DTOs.Requests;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/customer-addresses")]
    public class CustomerAddressesController : ControllerBase
    {
        private readonly ICustomerAddressService _customerAddressService;

        public CustomerAddressesController(ICustomerAddressService customerAddressService)
        {
            _customerAddressService = customerAddressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _customerAddressService.GetByAccountIdAsync(GetAccountId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _customerAddressService.GetByIdAsync(id, GetAccountId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerAddressRequest request)
        {
            var result = await _customerAddressService.CreateAsync(GetAccountId(), request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateCustomerAddressRequest request)
        {
            var result = await _customerAddressService.UpdateAsync(id, GetAccountId(), request);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _customerAddressService.DeleteAsync(id, GetAccountId());
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}/default")]
        public async Task<IActionResult> SetDefault(string id)
        {
            var result = await _customerAddressService.SetDefaultAsync(id, GetAccountId());
            return StatusCode(result.StatusCode, result);
        }

        private string GetAccountId()
        {
            return User.FindFirst("accountId")?.Value ?? string.Empty;
        }
    }
}
