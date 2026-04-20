using Application.DTOs;
using Application.Interfaces;
using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;    
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Application.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        public CatalogService(IProductRepository productRepository, IMapper mapper, IDistributedCache cache)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
        }
        public async Task<ApiResponse<IEnumerable<ProductDTO>>> GetAllProductsAsync()
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

        public async Task<ApiResponse<ProductDTO>> GetProductByIdAsync(string id)
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

        public async Task<ApiResponse<ProductDTO>> CreateProductAsync(CreateProductDTO productDto)
        {
            var product = _mapper.Map<Product>(productDto);
            product.Id = IdGenerator.GenerateId(ClassPrefix.Product);
            product.CreatedAt = DateTimeOffset.UtcNow;

            var createdProduct = await _productRepository.CreateProductAsync(product);
            if(createdProduct == null)
                return ApiResponse<ProductDTO>.Failure("Tạo sản phẩm thất bại.", 500);

            var dto = _mapper.Map<ProductDTO>(createdProduct);

            // Invalidate cache
            await _cache.RemoveAsync("all_products");
            return ApiResponse<ProductDTO>.Success(dto, "Tạo sản phẩm thành công.", 201);
        }
    }
}
