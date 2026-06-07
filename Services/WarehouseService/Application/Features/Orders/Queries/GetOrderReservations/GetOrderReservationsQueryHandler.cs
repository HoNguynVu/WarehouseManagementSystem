using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using MediatR;
using SharedLibrary.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using System.Linq;

namespace Application.Features.Orders.Queries.GetOrderReservations
{
    public class GetOrderReservationsQueryHandler : IRequestHandler<GetOrderReservationsQuery, ApiResponse<IEnumerable<StockReservationDTO>>>
    {
        private readonly IWarehouseUow _warehouseUow;
        private readonly IMapper _mapper;
        // Cần dùng DbContext trực tiếp nếu IWarehouseRepository chưa support Include(Warehouse)
        private readonly WarehouseDbContext _context; 

        public GetOrderReservationsQueryHandler(IWarehouseUow warehouseUow, IMapper mapper, WarehouseDbContext context)
        {
            _warehouseUow = warehouseUow;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<StockReservationDTO>>> Handle(GetOrderReservationsQuery request, CancellationToken cancellationToken)
        {
            // Lấy danh sách reservations kèm thông tin Warehouse
            var reservations = await _context.StockReservations
                .Include(r => r.Warehouse)
                .Where(r => r.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var dtos = _mapper.Map<IEnumerable<StockReservationDTO>>(reservations);

            return ApiResponse<IEnumerable<StockReservationDTO>>.Success(dtos, "Lấy thông tin điều phối kho thành công.");
        }
    }
}
