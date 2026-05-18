using Application.DTOs;
using Application.Interfaces;
using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using SharedLibrary.Responses;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CategoryService> _logger;
        public CategoryService(ICategoryRepository categoryRepository, IProductRepository productRepository, IMapper mapper, IDistributedCache cache, ILogger<CategoryService> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }
        public async Task<ApiResponse<IEnumerable<CategoryDTO>>> GetAllCategoriesAsync()
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

        public async Task<ApiResponse<CategoryDTO>> GetCategoryByIdAsync(string id)
        {
            try
            {
                string cacheKey = $"category_{id}";

                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var dtoFromCache = JsonSerializer.Deserialize<CategoryDTO>(cachedData);
                    return ApiResponse<CategoryDTO>.Success(dtoFromCache!, "Lấy danh mục từ cache.");
                }

                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    return ApiResponse<CategoryDTO>.Failure($"Không tìm thấy danh mục với ID: {id}", 404);
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

        public async Task<ApiResponse<CategoryDTO>> CreateCategoryAsync(CreateCategoryDTO createDTO)
        {
            try
            {
                var category = _mapper.Map<Category>(createDTO);
                category.Id = IdGenerator.GenerateId(ClassPrefix.Category);
                category.CreatedAt = DateTimeOffset.UtcNow;

                await _categoryRepository.CreateCategoryAsync(category);

                var dto = _mapper.Map<CategoryDTO>(category);

                await _cache.RemoveAsync("all_categories");
                return ApiResponse<CategoryDTO>.Success(dto, "Tạo danh mục thành công", 201);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục: {Message}", ex.Message);
                return ApiResponse<CategoryDTO>.Failure($"Lỗi hệ thống khi tạo danh mục: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<CategoryDTO>> UpdateCategoryAsync(string id, UpdateCategoryDTO updateDTO)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(id);

                if (existingCategory == null)
                {
                    return ApiResponse<CategoryDTO>.Failure($"Không tìm thấy danh mục với ID: {id}", 404);
                }

                string oldName = existingCategory.Name;

                _mapper.Map(updateDTO, existingCategory);
                existingCategory.UpdatedAt = DateTimeOffset.UtcNow;

                await _categoryRepository.UpdateCategoryAsync(existingCategory);
                var dto = _mapper.Map<CategoryDTO>(existingCategory);

                if (!string.IsNullOrWhiteSpace(updateDTO.Name) && updateDTO.Name != oldName)
                {
                    await _productRepository.UpdateCategoryNameForAllProductsAsync(id, updateDTO.Name);
                    await _cache.RemoveAsync("all_products");
                }

                await _cache.RemoveAsync($"category_{id}");
                await _cache.RemoveAsync("all_categories");

                _logger.LogInformation("Cập nhật thành công danh mục: {CategoryId}", id);
                return ApiResponse<CategoryDTO>.Success(dto, "Cập nhật danh mục thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục: {Message}", ex.Message);
                return ApiResponse<CategoryDTO>.Failure($"Lỗi hệ thống khi cập nhật danh mục: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<bool>> DeleteCategoryAsync(string id)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(id);
                if (existingCategory == null)
                {
                    return ApiResponse<bool>.Failure($"Không tìm thấy danh mục với ID: {id}", 404);
                }

                await _categoryRepository.DeleteCategoryAsync(id);

                await _cache.RemoveAsync($"category_{id}");
                await _cache.RemoveAsync("all_categories");

                _logger.LogInformation("Xóa thành công danh mục: {CategoryId}", id);
                return ApiResponse<bool>.Success(true, "Xóa danh mục thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục với Id: {CategoryId}, Message: {Message}", id, ex.Message);
                return ApiResponse<bool>.Failure($"Lỗi hệ thống khi xóa danh mục: {ex.Message}", 500);
            }
        }
    }
}
