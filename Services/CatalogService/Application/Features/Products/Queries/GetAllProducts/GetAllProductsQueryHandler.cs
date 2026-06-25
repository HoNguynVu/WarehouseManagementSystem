using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System.Text.Json;

namespace Application.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, ApiResponse<IEnumerable<ProductDTO>>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetAllProductsQueryHandler> _logger;

        public GetAllProductsQueryHandler(IProductRepository productRepository, IMapper mapper, IDistributedCache cache, ILogger<GetAllProductsQueryHandler> logger)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<ProductDTO>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
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
    }
}
