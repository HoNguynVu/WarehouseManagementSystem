using Application.Helpers;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;
using SharedLibrary.Exceptions;
using Domain.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Orders.Commands
{
    public class AllocateOrderCommandHandler : IRequestHandler<AllocateOrderCommand, ApiResponse<bool>>
    {
        private readonly IWarehouseUow _uow;
        private readonly IPublishEndpoint _publishEndpoint;

        public AllocateOrderCommandHandler(IWarehouseUow uow, IPublishEndpoint publishEndpoint)
        {
            _uow = uow;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<bool>> Handle(AllocateOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var item in request.Items)
                {
                    int remainingToAllocate = item.RequiredQuantity;

                    var warehouses = await _uow.Warehouse.GetWarehousesContainingProductAsync(item.ProductId);

                    var inventoriesToUse = warehouses
                        .SelectMany(w => w.Inventories)
                        .Where(i => i.ProductId == item.ProductId && (i.Quantity - i.ReservedQuantity) > 0)
                        .OrderByDescending(i => i.Quantity - i.ReservedQuantity)
                        .ToList();

                    foreach (var inv in inventoriesToUse)
                    {
                        if (remainingToAllocate <= 0) break;

                        int available = inv.Quantity - inv.ReservedQuantity;
                        int takeQty = Math.Min(available, remainingToAllocate);

                        inv.ReservedQuantity += takeQty;
                        remainingToAllocate -= takeQty;

                        var reservation = new StockReservation
                        {
                            Id = IdGenerator.GenerateId(ClassPrefix.OrderReservation),
                            OrderId = request.OrderId,
                            ProductId = item.ProductId,
                            WarehouseId = inv.WarehouseId,
                            Quantity = takeQty
                        };

                        // Đăng ký Domain Event
                        reservation.AddDomainEvent(new StockReservedEvent(request.OrderId, item.ProductId, inv.WarehouseId, takeQty));

                        await _uow.Warehouse.AddReservationAsync(reservation);
                    }

                    if (remainingToAllocate > 0)
                    {
                        throw new BadRequestException($"Không đủ hàng cho sản phẩm: {item.ProductId}. Còn thiếu: {remainingToAllocate}");
                    }
                }

                await _publishEndpoint.Publish(new InventoryAllocatedEvent
                {
                    OrderId = request.OrderId
                }, cancellationToken);

                var saved = await _uow.SaveChangeAsync(cancellationToken);

                if (!saved) throw new BadRequestException("Lưu dữ liệu thất bại.");

                return ApiResponse<bool>.Success(true, "Đã tách đơn và giữ kho thành công!");
            }
            catch (BadRequestException ex)
            {
                _uow.ClearTracker();

                await _publishEndpoint.Publish(new InventoryAllocationFailedEvent
                {
                    OrderId = request.OrderId,
                    Reason = ex.Message
                }, cancellationToken);

                return ApiResponse<bool>.Failure(ex.Message, 400);
            }
            catch (DbUpdateConcurrencyException)
            {
                _uow.ClearTracker();

                await _publishEndpoint.Publish(new InventoryAllocationFailedEvent
                {
                    OrderId = request.OrderId,
                    Reason = "Hệ thống đang bận do có nhiều người cùng mua sản phẩm này cùng lúc."
                }, cancellationToken);

                return ApiResponse<bool>.Failure("Concurrency conflict", 409);
            }
            catch (Exception ex)
            { 
                _uow.ClearTracker();    

                await _publishEndpoint.Publish(new InventoryAllocationFailedEvent
                {
                    OrderId = request.OrderId,
                    Reason = ex.Message
                }, cancellationToken);

                return ApiResponse<bool>.Failure(ex.Message, 500);
            }
        }
    }
}