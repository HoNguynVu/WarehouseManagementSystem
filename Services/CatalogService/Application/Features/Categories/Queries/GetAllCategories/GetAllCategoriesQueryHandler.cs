using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System.Text.Json;

namespace Application.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, ApiResponse<IEnumerable<CategoryDTO>>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<GetAllCategoriesQueryHandler> _logger;

        public GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<GetAllCategoriesQueryHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<CategoryDTO>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                string cacheKey = "all_categories";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtosFromCache = JsonSerializer.Deserialize<IEnumerable<CategoryDTO>>(cachedData);
                    return ApiResponse<IEnumerable<CategoryDTO>>.Success(dtosFromCache!, "Lấy danh sách danh mục từ cache.");
                }

                var categories = await _categoryRepository.GetAllAsync();
                var dtos = _mapper.Map<IEnumerable<CategoryDTO>>(categories);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dtos), cacheOptions);
                return ApiResponse<IEnumerable<CategoryDTO>>.Success(dtos, "Lấy danh sách danh mục thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách danh mục: {Message}", ex.Message);
                return ApiResponse<IEnumerable<CategoryDTO>>.Failure($"Lỗi hệ thống khi lấy danh sách danh mục: {ex.Message}", 500);
            }
        }
    }
}
