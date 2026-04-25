using Application.DTOs;
using Application.Interfaces;
using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;    
using SharedLibrary.Responses;
using SharedLibrary.IntergrationEvents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MassTransit;

namespace Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProductService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;
        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<ProductService> logger, IPublishEndpoint publishEndpoint)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }
        public async Task<ApiResponse<IEnumerable<ProductDTO>>> GetAllProductsAsync()
        {
            try
            {
                string cacheKey = "all_products";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtosFromCache = JsonSerializer.Deserialize<IEnumerable<ProductDTO>>(cachedData);
                    return ApiResponse<IEnumerable<ProductDTO>>.Success(dtosFromCache!, "Lấy danh sách sản phẩm từ cache.");
                }

                var products = await _productRepository.GetAllAsync();
                var dtos = _mapper.Map<IEnumerable<ProductDTO>>(products);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), cacheOptions);

                return ApiResponse<IEnumerable<ProductDTO>>.Success(dtos, "Lấy danh sách sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sản phẩm: {Message}", ex.Message);
                return ApiResponse<IEnumerable<ProductDTO>>.Failure($"Lỗi hệ thống khi lấy danh sách sản phẩm: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<ProductDTO>> GetProductByIdAsync(string id)
        {
            try
            {
                string cacheKey = $"product_{id}";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtoFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedData);
                    return ApiResponse<ProductDTO>.Success(dtoFromCache!, "Lấy sản phẩm từ cache.");
                }

                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                    return ApiResponse<ProductDTO>.Failure($"Không tìm thấy sản phẩm với ID: {id}", 404);

                var dto = _mapper.Map<ProductDTO>(product);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);
                return ApiResponse<ProductDTO>.Success(dto, "Lấy sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy sản phẩm với ID {Id}: {Message}", id, ex.Message);
                return ApiResponse<ProductDTO>.Failure($"Lỗi hệ thống khi lấy sản phẩm: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<ProductDTO>> CreateProductAsync(CreateProductDTO productDto)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(productDto.CategoryId);
                if (category == null)
                    return ApiResponse<ProductDTO>.Failure($"Danh mục với ID {productDto.CategoryId} không tồn tại.", 400);

                var product = _mapper.Map<Product>(productDto);
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

        public async Task<ApiResponse<ProductDTO>> UpdateProductAsync(string id, UpdateProductDTO productDto)
        {
            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                    return ApiResponse<ProductDTO>.Failure($"Không tìm thấy sản phẩm với ID: {id}", 404);
                
                string oldProductName = existingProduct.Name;

                if (!string.IsNullOrEmpty(productDto.CategoryId) && productDto.CategoryId != existingProduct.CategoryId)
                {
                    var category = await _categoryRepository.GetByIdAsync(productDto.CategoryId);
                    if (category == null)
                        return ApiResponse<ProductDTO>.Failure("Danh mục mới không tồn tại.", 400);

                    existingProduct.CategoryName = category.Name;
                }

                _mapper.Map(productDto, existingProduct);
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
                    });
                }

                await _cache.RemoveAsync("all_products");
                await _cache.RemoveAsync($"product_{id}");

                _logger.LogInformation("Cập nhật thành công sản phẩm: {ProductId}", id);
                return ApiResponse<ProductDTO>.Success(dto, "Câp nhật sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm với ID {Id}: {Message}", id, ex.Message);
                return ApiResponse<ProductDTO>.Failure($"Lỗi hệ thống khi cập nhật sản phẩm: {ex.Message}", 500);

            }
        }

        public async Task<ApiResponse<bool>> DeleteProductAsync(string id)
        {
            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                    return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với ID: {id}", 404);

                var isDeleted = await _productRepository.DeleteProductAsync(id);
                if (!isDeleted)
                    return ApiResponse<bool>.Failure("Xóa sản phẩm thất bại.", 400);

                await _cache.RemoveAsync("all_products");
                await _cache.RemoveAsync($"product_{id}");

                _logger.LogInformation("Xóa thành công sản phẩm: {ProductId}", id);
                return ApiResponse<bool>.Success(true, "Xóa sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm với ID {Id}: {Message}", id, ex.Message);
                return ApiResponse<bool>.Failure($"Lỗi hệ thống khi xóa sản phẩm: {ex.Message}", 500);
            }
        }
    }
}
