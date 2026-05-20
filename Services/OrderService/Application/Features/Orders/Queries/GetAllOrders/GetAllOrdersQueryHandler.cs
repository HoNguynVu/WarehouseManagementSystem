using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, ApiResponse<IEnumerable<OrderDTO>>>
    {
        private readonly IOrderUow _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllOrdersQueryHandler> _logger;

        public GetAllOrdersQueryHandler(IOrderUow uow, IMapper mapper, ILogger<GetAllOrdersQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<OrderDTO>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var orders = await _uow.Orders.GetAllAsync();
                var dtos = _mapper.Map<IEnumerable<OrderDTO>>(orders);
                return ApiResponse<IEnumerable<OrderDTO>>.Success(dtos, "Orders retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return ApiResponse<IEnumerable<OrderDTO>>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
