using Application.DTOs;
using Application.Helpers;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, ApiResponse<CategoryDTO>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CreateCategoryCommandHandler> _logger;

        public CreateCategoryCommandHandler(ICategoryRepository categoryRepository, IMapper mapper, IDistributedCache cache, ILogger<CreateCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<CategoryDTO>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var category = _mapper.Map<Category>(request.CategoryDto);
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
    }
}
