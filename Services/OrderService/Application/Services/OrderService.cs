using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using Microsoft.Extensions.Logging;
using SharedLibrary.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderUow _uow;
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderUow uow,
            IPaymentService paymentService,
            IMapper mapper,
            ILogger<OrderService> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderDTO>> CreateOrderAsync(CreateOrderDTO dto, string accountId)
        {
            try
            {
                if (dto.Items == null || !dto.Items.Any())
                    return ApiResponse<OrderDTO>.Failure("Order must have at least one item", 400);

                await _uow.BeginTransactionAsync();

                var order = new Order
                {
                    Id = IdGenerator.GenerateId(PaymentConstants.PrefixOrder),
                    AccountId = accountId,
                    PaymentMethod = dto.PaymentMethod,
                    Status = PaymentConstants.StatusPending,
                    CreatedAt = DateTimeOffset.UtcNow,
                    TotalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice)
                };

                _uow.Orders.Create(order);

                var orderItems = dto.Items.Select(item => new OrderItem
                {
                    Id = IdGenerator.GenerateId("ITM"),
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToList();

                foreach (var item in orderItems)
                    _uow.OrderItems.Create(item);

                await _uow.CommitAsync();

                // CreateOrderEvent is published only after payment is confirmed (in PaymentService.ProcessCallback)
                PaymentLinkDTO? paymentInfo = null;
                if (dto.PaymentMethod == PaymentConstants.MethodZaloPay)
                {
                    var paymentResult = await _paymentService.CreateZaloPayLinkForOrder(order.Id, order.TotalAmount);
                    paymentInfo = paymentResult.dto;
                }

                var orderDto = _mapper.Map<OrderDTO>(order);
                orderDto.OrderItems = _mapper.Map<List<OrderItemDTO>>(orderItems);
                orderDto.PaymentInfo = paymentInfo;

                return ApiResponse<OrderDTO>.Success(orderDto, "Order created successfully", 201);
            }
            catch (Exception ex)
            {
                if (ex.Message != "No transaction in progress.")
                    await _uow.RollbackAsync();
                _logger.LogError(ex, "Error creating order");
                return ApiResponse<OrderDTO>.Failure($"System error: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<OrderDTO>> GetOrderByIdAsync(string id)
        {
            try
            {
                var order = await _uow.Orders.GetByIdAsync(id);
                if (order == null)
                    return ApiResponse<OrderDTO>.Failure($"Order with ID {id} not found", 404);

                var orderItems = await _uow.OrderItems.GetByOrderId(id);
                
                var dto = _mapper.Map<OrderDTO>(order);
                dto.OrderItems = _mapper.Map<List<OrderItemDTO>>(orderItems);

                return ApiResponse<OrderDTO>.Success(dto, "Order retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {Id}", id);
                return ApiResponse<OrderDTO>.Failure($"System error: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<IEnumerable<OrderDTO>>> GetAllOrdersAsync()
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
