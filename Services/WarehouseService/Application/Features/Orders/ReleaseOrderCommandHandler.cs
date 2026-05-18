using MediatR;
using SharedLibrary.Responses;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Orders
{
    public class ReleaseOrderCommandHandler : IRequestHandler<ReleaseOrderCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _uow;
        private readonly WarehouseDbContext _dbContext;

        public ReleaseOrderCommandHandler(IWarehouseUow uow, WarehouseDbContext dbContext)
        {
            _uow = uow;
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<bool>> Handle(ReleaseOrderCommand request, CancellationToken cancellationToken)
        {
            await _uow.BeginTransactionAsync();

            try
            {
                // 1. Tìm TẤT CẢ biên lai thuộc về Đơn hàng này
                var reservations = await _dbContext.StockReservations
                    .Where(r => r.OrderId == request.OrderId)
                    .ToListAsync(cancellationToken);

                // Nếu không có biên lai nào, coi như đã hủy hoặc đơn không tồn tại (Idempotent)
                if (!reservations.Any())
                {
                    await _uow.RollbackAsync(); // Không có gì để làm thì Rollback cho an toàn
                    return ApiResponse<bool>.Success(true, "Không tìm thấy dữ liệu giữ kho cho đơn hàng này (Có thể đã được nhả trước đó).");
                }

                // 2. Duyệt qua từng tờ biên lai để trả hàng lại đúng kho
                foreach (var res in reservations)
                {
                    // Lôi đúng cái kho và đúng cái sản phẩm trong tờ biên lai ra
                    var inventory = await _dbContext.Inventories
                        .FirstOrDefaultAsync(i => i.WarehouseId == res.WarehouseId && i.ProductId == res.ProductId, cancellationToken);

                    if (inventory != null)
                    {
                        // Trả lại số lượng đã giữ
                        inventory.ReservedQuantity -= res.Quantity;

                        // Đảm bảo không bao giờ bị âm (phòng hờ lỗi data cũ)
                        if (inventory.ReservedQuantity < 0) inventory.ReservedQuantity = 0;
                    }
                }

                // 3. Xé bỏ các tờ biên lai (Xóa khỏi DB)
                _dbContext.StockReservations.RemoveRange(reservations);

                // 4. Lưu thay đổi và chốt Transaction
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _uow.CommitAsync();

                return ApiResponse<bool>.Success(true, $"Đã hủy đơn {request.OrderId} và hoàn trả tồn kho thành công!");
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message, 500);
            }
        }
    }
}