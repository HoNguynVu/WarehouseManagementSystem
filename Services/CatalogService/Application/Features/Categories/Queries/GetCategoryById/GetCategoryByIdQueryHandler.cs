using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System.Text.Json;

namespace Application.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, ApiResponse<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetCategoryByIdQueryHandler> _logger;

        public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<GetCategoryByIdQueryHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<CategoryDTO>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                string cacheKey = "category_{request.Id}";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtoFromCache = JsonSerializer.Deserialize<CategoryDTO>(cachedData);
                    return ApiResponse<CategoryDTO>.Success(dtoFromCache!, "Lấy danh mục từ cache.");
                }

                var category = await _categoryRepository.GetByIdAsync(request.Id);
                if (category == null)
                {
                    return ApiResponse<CategoryDTO>.Failure($"Không tìm thấy danh mục với ID: {request.Id}", 404);
                }

                var dto = _mapper.Map<CategoryDTO>(category);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), cacheOptions);
                return ApiResponse<CategoryDTO>.Success(dto, "Lấy danh mục thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh mục: {Message}", ex.Message);
                return ApiResponse<CategoryDTO>.Failure($"Lỗi hệ thống khi lấy danh mục: {ex.Message}", 500);
            }
        }
    }
}
