using Application.DTOs;
using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreateProductCommandHandler> _logger;

        public CreateProductCommandHandler(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<CreateProductCommandHandler> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<ProductDTO>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(request.ProductDto.CategoryId);
                if (category == null)
                    return ApiResponse<ProductDTO>.Failure($"Danh mục với ID {request.ProductDto.CategoryId} không tồn tại.", 400);

                var product = _mapper.Map<Product>(request.ProductDto);
                product.CategoryName = category.Name;
                product.Id = IdGenerator.GenerateId(ClassPrefix.Product);
                product.CreatedAt = DateTimeOffset.UtcNow;

                var createdProduct = await _productRepository.CreateProductAsync(product);

                var dto = _mapper.Map<ProductDTO>(createdProduct);

                // Invalidate cache
                await _cache.RemoveAsync("all_products");
                return ApiResponse<ProductDTO>.Success(dto, "Tạo sản phẩm thành công.", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo sản phẩm: {Message}", ex.Message);
                return ApiResponse<ProductDTO>.Failure($"Lỗi hệ thống khi tạo sản phẩm: {ex.Message}", 500);
            }
        }
    }
}
