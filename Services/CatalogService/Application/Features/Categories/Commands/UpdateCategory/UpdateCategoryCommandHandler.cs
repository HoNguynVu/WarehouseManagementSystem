using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, ApiResponse<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<UpdateCategoryCommandHandler> _logger;

        public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository, IProductRepository productRepository, IMapper mapper, IDistributedCache cache, ILogger<UpdateCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<CategoryDTO>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(request.Id);

                if (existingCategory == null)
                {
                    return ApiResponse<CategoryDTO>.Failure($"Không tìm thấy danh mục với ID: {request.Id}", 404);
                }

                string oldName = existingCategory.Name;

                _mapper.Map(request.CategoryDto, existingCategory);
                existingCategory.UpdatedAt = DateTimeOffset.UtcNow;

                await _categoryRepository.UpdateCategoryAsync(existingCategory);
                var dto = _mapper.Map<CategoryDTO>(existingCategory);

                if (!string.IsNullOrWhiteSpace(request.CategoryDto.Name) && request.CategoryDto.Name != oldName)
                {
                    await _productRepository.UpdateCategoryNameForAllProductsAsync(request.Id, request.CategoryDto.Name);
                    await _cache.RemoveAsync("all_products");
                }

                await _cache.RemoveAsync($"category_{request.Id}");
                await _cache.RemoveAsync("all_categories");

                _logger.LogInformation("Cập nhật thành công danh mục: {CategoryId}", request.Id);
                return ApiResponse<CategoryDTO>.Success(dto, "Cập nhật danh mục thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục: {Message}", ex.Message);
                return ApiResponse<CategoryDTO>.Failure($"Lỗi hệ thống khi cập nhật danh mục: {ex.Message}", 500);
            }
        }
    }
}
