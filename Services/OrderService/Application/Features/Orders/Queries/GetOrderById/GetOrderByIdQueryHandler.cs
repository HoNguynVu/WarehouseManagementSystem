using Application.DTOs;
using AutoMapper;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;

namespace Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, ApiResponse<OrderDTO>>
    {
        private readonly IOrderUow _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<GetOrderByIdQueryHandler> _logger;

        public GetOrderByIdQueryHandler(IOrderUow uow, IMapper mapper, ILogger<GetOrderByIdQueryHandler> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderDTO>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(request.Id);
                if (order == null)
                    return ApiResponse<OrderDTO>.Failure($"Order with ID {request.Id} not found", 404);

                var orderItems = await _uow.OrderItems.GetByOrderId(request.Id);
                
                // Lấy thêm thông tin Payment
                var payment = await _uow.Payments.GetByOrderIdAsync(request.Id);

                var dto = _mapper.Map<OrderDTO>(order);
                dto.OrderItems = _mapper.Map<List<OrderItemDTO>>(orderItems);
                
                if (payment != null)
                {
                    dto.PaymentInfo = new PaymentLinkDTO
                    {
                        IsSuccess = payment.Status == "Completed" || payment.Status == "Paid",
                        PaymentId = payment.TransactionId,
                        PaymentUrl = "", // Url chỉ có lúc mới tạo
                        Message = $"Payment Status: {payment.Status}"
                    };
                }

                return ApiResponse<OrderDTO>.Success(dto, "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {Id}", request.Id);
                return ApiResponse<OrderDTO>.Failure($"System error: {ex.Message}", 500);
            }
        }
    }
}
