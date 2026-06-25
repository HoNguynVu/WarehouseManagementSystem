using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UpdateProductCommandHandler> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateProductCommandHandler(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<UpdateProductCommandHandler> logger, IPublishEndpoint publishEndpoint)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<ProductDTO>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(request.Id);
                if (existingProduct == null)
                    return ApiResponse<ProductDTO>.Failure($"Không tìm thấy sản phẩm với ID: {request.Id}", 404);
                
                string oldProductName = existingProduct.Name;

                if (!string.IsNullOrEmpty(request.ProductDto.CategoryId) && request.ProductDto.CategoryId != existingProduct.CategoryId)
                {
                    var category = await _categoryRepository.GetByIdAsync(request.ProductDto.CategoryId);
                    if (category == null)
                        return ApiResponse<ProductDTO>.Failure("Danh mục mới không tồn tại.", 400);

                    existingProduct.CategoryName = category.Name;
                }

                _mapper.Map(request.ProductDto, existingProduct);
                existingProduct.UpdatedAt = DateTimeOffset.UtcNow;

                var isUpdated = await _productRepository.UpdateProductAsync(existingProduct);
                if (!isUpdated)
                    return ApiResponse<ProductDTO>.Failure("Không có thay đổi nào được lưu.", 400);

                var dto = _mapper.Map<ProductDTO>(existingProduct);

                // Gửi sự kiện cập nhật sản phẩm
                if(oldProductName != existingProduct.Name) 
                {
                    await _publishEndpoint.Publish(new UpdateProductEvent
                    {
                        ProductId = existingProduct.Id,
                        ProductName = existingProduct.Name
                    }, cancellationToken);
                }

                await _cache.RemoveAsync("all_products");
                await _cache.RemoveAsync($"product_{request.Id}");

                _logger.LogInformation("Cập nhật thành công sản phẩm: {ProductId}", request.Id);
                return ApiResponse<ProductDTO>.Success(dto, "Cập nhật sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với ID {Id}: {Message}", request.Id, ex.Message);
                return ApiResponse<ProductDTO>.Failure($"Lỗi hệ thống khi cập nhật sản phẩm: {ex.Message}", 500);
            }
        }
    }
}
