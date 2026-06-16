using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Orders.Queries.GetOrdersByAccountId
{
    public class GetOrdersByAccountIdQueryHandler : IRequestHandler<GetOrdersByAccountIdQuery, ApiResponse<IEnumerable<OrderDTO>>>
    {
        private readonly IOrderUow _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetOrdersByAccountIdQueryHandler> _logger;

        public GetOrdersByAccountIdQueryHandler(IOrderUow uow, IMapper mapper, ILogger<GetOrdersByAccountIdQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<IEnumerable<OrderDTO>>> Handle(GetOrdersByAccountIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.AccountId))
                {
                    return ApiResponse<IEnumerable<OrderDTO>>.Failure("AccountId is required.", 400);
                }

                var orders = await _uow.Orders.GetByAccountIdAsync(request.AccountId);
                var dtos = _mapper.Map<IEnumerable<OrderDTO>>(orders);
                
                return ApiResponse<IEnumerable<OrderDTO>>.Success(dtos, "Orders retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for AccountId {AccountId}", request.AccountId);
                return ApiResponse<IEnumerable<OrderDTO>>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
