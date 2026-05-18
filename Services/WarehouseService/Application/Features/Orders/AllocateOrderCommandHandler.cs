using MediatR;
using SharedLibrary.Responses;
using Domain.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Orders
{
    public class AllocateOrderCommandHandler : IRequestHandler<AllocateOrderCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _uow;
        private readonly WarehouseDbContext _dbContext;

        public AllocateOrderCommandHandler(IWarehouseUow uow, WarehouseDbContext dbContext)
        {
            _uow = uow;
            _dbContext = dbContext;
        }

        public async Task<ApiResponse<bool>> Handle(AllocateOrderCommand request, CancellationToken cancellationToken)
        {
            await _uow.BeginTransactionAsync();

            try
            {

                foreach (var item in request.Items)
                {
                    int remainingToAllocate = item.RequiredQuantity;

                    // Lấy danh sách các kho có sản phẩm này, kèm tồn kho hiện tại
                    var warehouses = await _uow.Warehouse.GetWarehousesContainingProductAsync(item.ProductId);

                    // Gom tồn kho của sản phẩm này ở tất cả các kho, ưu tiên kho nhiều hàng nhất
                    var inventoriesToUse = warehouses
                        .SelectMany(w => w.Inventories)
                        .Where(i => i.ProductId == item.ProductId && (i.Quantity - i.ReservedQuantity) > 0)
                        .OrderByDescending(i => i.Quantity - i.ReservedQuantity)
                        .ToList();

                    // Thuật toán băm nhỏ số lượng chia cho các kho
                    foreach (var inv in inventoriesToUse)
                    {
                        if (remainingToAllocate <= 0) break;

                        int available = inv.Quantity - inv.ReservedQuantity;
                        int takeQty = Math.Min(available, remainingToAllocate);

                        inv.ReservedQuantity += takeQty; // Giữ kho
                        remainingToAllocate -= takeQty;

                        var reservation = new StockReservation
                        {
                            Id = IdGenerator.GenerateId(ClassPrefix.OrderReservation),
                            OrderId = request.OrderId,
                            ProductId = item.ProductId,
                            WarehouseId = inv.WarehouseId,
                            Quantity = takeQty,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _dbContext.StockReservations.AddAsync(reservation, cancellationToken);
                    }

                    if (remainingToAllocate > 0)
                    {
                        throw new Exception($"Không đủ hàng cho sản phẩm: {item.ProductId}. Còn thiếu: {remainingToAllocate}");
                    }
                }

                // Lưu xuống DB (Entity Framework sẽ tự check RowVersion để chống tranh chấp)
                var saved = await _uow.Warehouse.SaveChangeAsync(cancellationToken);
                if (!saved) throw new Exception("Lưu dữ liệu thất bại.");

                await _uow.CommitAsync();
                return ApiResponse<bool>.Success(true, "Đã tách đơn và giữ kho thành công!");
            }
            catch (DbUpdateConcurrencyException)
            {
                // Bắt lỗi khi 2 khách hàng giành nhau món hàng cuối cùng
                await _uow.RollbackAsync();
                return ApiResponse<bool>.Failure("Hệ thống đang bận vì có nhiều người cùng mua, vui lòng thử lại!", 409);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
        }
    }
}