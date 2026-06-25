using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Categories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ApiResponse<bool>>
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DeleteCategoryCommandHandler> _logger;

        public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IDistributedCache cache, ILogger<DeleteCategoryCommandHandler> logger)
        {
            _categoryRepository = categoryRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingCategory = await _categoryRepository.GetByIdAsync(request.Id);
                if (existingCategory == null)
                {
                    return ApiResponse<bool>.Failure($"Không tìm thấy danh mục với ID: {request.Id}", 404);
                }

                await _categoryRepository.DeleteCategoryAsync(request.Id);

                await _cache.RemoveAsync($"category_{request.Id}");
                await _cache.RemoveAsync("all_categories");

                _logger.LogInformation("Xóa thành công danh mục: {CategoryId}", request.Id);
                return ApiResponse<bool>.Success(true, "Xóa danh mục thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục với Id: {CategoryId}, Message: {Message}", request.Id, ex.Message);
                return ApiResponse<bool>.Failure($"Lỗi hệ thống khi xóa danh mục: {ex.Message}", 500);
            }
        }
    }
}
