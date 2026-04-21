using Microsoft.AspNetCore.Mvc;
using Application.Interfaces;
using Application.DTOs;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _productService.GetAllProductsAsync();
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _productService.GetProductByIdAsync(id);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDTO dto)
        {
            var response = await _productService.CreateProductAsync(dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, UpdateProductDTO dto)
        {
            var response = await _productService.UpdateProductAsync(id, dto);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var response = await _productService.DeleteProductAsync(id);
            if (!response.IsSuccess)
            {
                return StatusCode(response.StatusCode, response);
            }
            return Ok(response);
        }
    }
}
