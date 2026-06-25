using Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Products.Commands.DeleteProduct
{
    public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ApiResponse<bool>>
    {
        private readonly IProductRepository _productRepository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<DeleteProductCommandHandler> _logger;

        public DeleteProductCommandHandler(IProductRepository productRepository, IDistributedCache cache, ILogger<DeleteProductCommandHandler> logger)
        {
            _productRepository = productRepository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(request.Id);
                if (existingProduct == null)
                    return ApiResponse<bool>.Failure($"Không tìm thấy sản phẩm với ID: {request.Id}", 404);

                var isDeleted = await _productRepository.DeleteProductAsync(request.Id);
                if (!isDeleted)
                    return ApiResponse<bool>.Failure("Xóa sản phẩm thất bại.", 400);

                await _cache.RemoveAsync("all_products");
                await _cache.RemoveAsync($"product_{request.Id}");

                _logger.LogInformation("Xóa thành công sản phẩm: {ProductId}", request.Id);
                return ApiResponse<bool>.Success(true, "Xóa sản phẩm thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm với ID {Id}: {Message}", request.Id, ex.Message);
                return ApiResponse<bool>.Failure($"Lỗi hệ thống khi xóa sản phẩm: {ex.Message}", 500);
            }
        }
    }
}
