using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Responses;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Application.Features.Orders.Queries.GetOrderState
{
    public class GetOrderStateQueryHandler : IRequestHandler<GetOrderStateQuery, ApiResponse<OrderStateDTO>>
    {
        private readonly OrderDbContext _context;

        public GetOrderStateQueryHandler(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<OrderStateDTO>> Handle(GetOrderStateQuery request, CancellationToken cancellationToken)
        {
            var state = await _context.OrderStates
                .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);

            if (state == null)
            {
                return ApiResponse<OrderStateDTO>.Failure("Không tìm thấy trạng thái Saga của đơn hàng này.", 404, new List<string> { "Saga state not found" });
            }

            var dto = new OrderStateDTO
            {
                OrderId = state.OrderId,
                CurrentState = state.CurrentState,
                IsPaid = state.IsPaid,
                IsStockAllocated = state.IsStockAllocated,
                CreatedAt = state.CreatedAt,
                UpdatedAt = state.UpdatedAt
            };

            return ApiResponse<OrderStateDTO>.Success(dto, "Lấy trạng thái đơn hàng thành công.");
        }
    }
}
