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

        public ReleaseOrderCommandHandler(IWarehouseUow uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<bool>> Handle(ReleaseOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Tìm TẤT CẢ biên lai thuộc về Đơn hàng này
                var reservations = await _uow.Warehouse.GetReservationsByOrderIdAsync(request.OrderId);

                // Nếu không có biên lai nào, coi như đã hủy hoặc đơn không tồn tại (Idempotent)
                if (!reservations.Any())
                {
                    return ApiResponse<bool>.Success(true, "Không tìm thấy dữ liệu giữ kho cho đơn hàng này (Có thể đã được nhả trước đó).");
                }

                // 2. Duyệt qua từng tờ biên lai để trả hàng lại đúng kho
                foreach (var res in reservations)
                {
                    // Lôi đúng cái kho và đúng cái sản phẩm trong tờ biên lai ra
                    var inventory = await _uow.Warehouse.GetInventoryAsync(res.WarehouseId, res.ProductId);

                    if (inventory != null)
                    {
                        // Trả lại số lượng đã giữ
                        inventory.ReservedQuantity -= res.Quantity;

                        // Đảm bảo không bao giờ bị âm (phòng hờ lỗi data cũ)
                        if (inventory.ReservedQuantity < 0) inventory.ReservedQuantity = 0;
                    }
                    _uow.Warehouse.DeleteReservation(res);
                }

                // 4. Lưu thay đổi và chốt Transaction
                await _uow.SaveChangeAsync(cancellationToken);

                return ApiResponse<bool>.Success(true, $"Đã hủy đơn {request.OrderId} và hoàn trả tồn kho thành công!");
            }
            catch (Exception ex)
            {
                _uow.ClearTracker();
                return ApiResponse<bool>.Failure(ex.Message, 500);
            }
        }
    }
}