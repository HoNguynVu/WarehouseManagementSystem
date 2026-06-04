using MassTransit;
using SharedLibrary.IntegrationEvents;
using Domain.Enums;
using Infrastructure.Sagas;
using System;

namespace Application.Sagas
{
    public class OrderStateMachine : MassTransitStateMachine<OrderState>
    {
        public State AllocatingStock { get; private set; }
        public State AwaitingPayment { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }
        public State Cancelled { get; private set; }

        // Events
        public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; }
        public Event<InventoryAllocatedEvent> InventoryAllocated { get; private set; }
        public Event<InventoryAllocationFailedEvent> InventoryAllocationFailed { get; private set; }
        public Event<PaymentSuccessEvent> PaymentSuccess { get; private set; }
        public Event<OrderCancelledEvent> OrderCancelled { get; private set; }

        public OrderStateMachine()
        {
            InstanceState(x => x.CurrentState);

            // Correlate events by OrderId
            Event(() => OrderSubmitted, x => x.CorrelateBy(state => state.OrderId, context => context.Message.OrderId)
                                              .SelectId(context => Guid.NewGuid()));
                                              
            Event(() => InventoryAllocated, x => x.CorrelateBy(state => state.OrderId, context => context.Message.OrderId));
            Event(() => InventoryAllocationFailed, x => x.CorrelateBy(state => state.OrderId, context => context.Message.OrderId));
            Event(() => PaymentSuccess, x => x.CorrelateBy(state => state.OrderId, context => context.Message.OrderId));
            Event(() => OrderCancelled, x => x.CorrelateBy(state => state.OrderId, context => context.Message.OrderId));

            Initially(
                When(OrderSubmitted)
                    .Then(context =>
                    {
                        context.Saga.OrderId = context.Message.OrderId;
                        context.Saga.AccountId = context.Message.AccountId;
                        context.Saga.TotalAmount = context.Message.TotalAmount;
                        context.Saga.CreatedAt = DateTime.UtcNow;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                        context.Saga.IsPaid = false;
                        context.Saga.IsStockAllocated = false;
                    })
                    .Publish(context => new AllocateOrderCommand
                    {
                        OrderId = context.Message.OrderId,
                        Items = context.Message.Items
                    })
                    .TransitionTo(AllocatingStock)
            );

            During(AllocatingStock,
                When(InventoryAllocated)
                    .Then(context =>
                    {
                        context.Saga.IsStockAllocated = true;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .IfElse(context => context.Saga.IsPaid,
                        x => x.Publish(context => new UpdateOrderStatusCommand { OrderId = context.Saga.OrderId, Status = OrderStatus.Completed })
                              .TransitionTo(Completed)
                              .Finalize(),
                        x => x.Publish(context => new UpdateOrderStatusCommand { OrderId = context.Saga.OrderId, Status = OrderStatus.AwaitingPayment })
                              .TransitionTo(AwaitingPayment)
                    ),

                When(InventoryAllocationFailed)
                    .Then(context =>
                    {
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new UpdateOrderStatusCommand 
                    { 
                        OrderId = context.Saga.OrderId, 
                        Status = OrderStatus.Failed, 
                        Reason = context.Message.Reason 
                    })
                    .TransitionTo(Failed)
                    .Finalize(),

                When(PaymentSuccess)
                    .Then(context =>
                    {
                        context.Saga.IsPaid = true;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
            );

            During(AwaitingPayment,
                When(PaymentSuccess)
                    .Then(context =>
                    {
                        context.Saga.IsPaid = true;
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new UpdateOrderStatusCommand { OrderId = context.Saga.OrderId, Status = OrderStatus.Completed })
                    .TransitionTo(Completed)
                    .Finalize(),

                When(OrderCancelled)
                    .Then(context =>
                    {
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .Publish(context => new ReleaseOrderStockCommand { OrderId = context.Saga.OrderId })
                    .Publish(context => new UpdateOrderStatusCommand { OrderId = context.Saga.OrderId, Status = OrderStatus.Cancelled })
                    .TransitionTo(Cancelled)
                    .Finalize()
            );

            DuringAny(
                When(OrderCancelled)
                    .Then(context =>
                    {
                        context.Saga.UpdatedAt = DateTime.UtcNow;
                    })
                    .If(context => context.Saga.IsStockAllocated,
                        x => x.Publish(context => new ReleaseOrderStockCommand { OrderId = context.Saga.OrderId })
                    )
                    .Publish(context => new UpdateOrderStatusCommand { OrderId = context.Saga.OrderId, Status = OrderStatus.Cancelled })
                    .TransitionTo(Cancelled)
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }
    }
}
