using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System.Text.Json;

namespace Application.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ApiResponse<ProductDTO>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetProductByIdQueryHandler> _logger;

        public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper, IDistributedCache cache, ILogger<GetProductByIdQueryHandler> logger)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<ProductDTO>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                string cacheKey = "product_{request.Id}";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtoFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedData);
                    return ApiResponse<ProductDTO>.Success(dtoFromCache!, "Lấy sản phẩm từ cache.");
                }

                var product = await _productRepository.GetByIdAsync(request.Id);
                if (product == null)
                    return ApiResponse<ProductDTO>.Failure($"Không tìm thấy sản phẩm với ID: {request.Id}", 404);

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
                _logger.LogError(ex, "Lỗi khi lấy sản phẩm với ID {Id}: {Message}", request.Id, ex.Message);
                return ApiResponse<ProductDTO>.Failure($"Lỗi hệ thống khi lấy sản phẩm: {ex.Message}", 500);
            }
        }
    }
}
