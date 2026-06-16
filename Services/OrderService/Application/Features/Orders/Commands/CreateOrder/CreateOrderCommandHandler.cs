using Application.DTOs;
using Application.Helpers;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.UnitOfWorks;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using SharedLibrary.IntegrationEvents;
using SharedLibrary.Responses;
using System.Linq;

namespace Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderDTO>>
    {
        private readonly IOrderUow _uow;
        private readonly IPaymentService _paymentService;
        private readonly IMapper _mapper;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IOrderUow uow,
            IPaymentService paymentService,
            IMapper mapper,
            IPublishEndpoint publishEndpoint,
            ILogger<CreateOrderCommandHandler> logger)
        {
            _uow = uow;
            _paymentService = paymentService;
            _mapper = mapper;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<ApiResponse<OrderDTO>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var dto = request.Dto;

                if (dto.Items == null || !dto.Items.Any())
                    return ApiResponse<OrderDTO>.Failure("Order must have at least one item", 400);

                await _uow.BeginTransactionAsync();

                var order = new Order
                {
                    Id = IdGenerator.GenerateId(PaymentConstants.PrefixOrder),
                    AccountId = request.AccountId,
                    PaymentMethod = dto.PaymentMethod,
                    ReceiverName = dto.ReceiverName,
                    ReceiverPhone = dto.ReceiverPhone,
                    ShippingAddress = dto.ShippingAddress,
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

                // Publish OrderSubmittedEvent to start Saga Orchestrator
                await _publishEndpoint.Publish(new OrderSubmittedEvent
                {
                    OrderId = order.Id,
                    AccountId = order.AccountId,
                    TotalAmount = order.TotalAmount,
                    Items = orderItems.Select(item => new OrderItemMessage
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    }).ToList()
                }, cancellationToken);

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
    }
}
