using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Sagas.StateMachineSaga;

/// <summary>
/// State machine-based saga for shipment preparation.
/// Can be initiated by either OrderConfirmed or InventoryReserved messages.
/// Completes when both messages have been received.
/// </summary>
public sealed class ShipmentPreparationStateMachine : MassTransitStateMachine<ShipmentPreparationState>
{
    private readonly ILogger<ShipmentPreparationStateMachine> _logger;

    public ShipmentPreparationStateMachine(ILogger<ShipmentPreparationStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        // Define events
        Event(() => OrderConfirmed, x => x.CorrelateById(context => context.Message.OrderId));
        Event(() => InventoryReserved, x => x.CorrelateById(context => context.Message.OrderId));

        // Initially, accept either event to start the saga
        Initially(
            When(OrderConfirmed)
                .Then(context =>
                {
                    context.Saga.OrderConfirmed = true;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.ConfirmedAt = context.Message.ConfirmedAt;
                    _logger.LogInformation(
                        "StateMachine: OrderConfirmed received for OrderId: {OrderId}",
                        context.Message.OrderId);
                    LogSagaState(context);
                })
                .TransitionTo(WaitingForInventory),
            When(InventoryReserved)
                .Then(context =>
                {
                    context.Saga.InventoryReserved = true;
                    context.Saga.ReservedAt = context.Message.ReservedAt;
                    _logger.LogInformation(
                        "StateMachine: InventoryReserved received for OrderId: {OrderId}",
                        context.Message.OrderId);
                    LogSagaState(context);
                })
                .TransitionTo(WaitingForOrder));

        // While waiting for inventory, accept InventoryReserved
        During(WaitingForInventory,
            When(InventoryReserved)
                .Then(context =>
                {
                    context.Saga.InventoryReserved = true;
                    context.Saga.ReservedAt = context.Message.ReservedAt;
                    _logger.LogInformation(
                        "StateMachine: InventoryReserved received (was waiting) for OrderId: {OrderId}",
                        context.Message.OrderId);
                    LogSagaState(context);
                })
                .Publish(context => new ShipmentPrepared
                {
                    OrderId = context.Saga.CorrelationId,
                    CustomerId = context.Saga.CustomerId ?? Guid.Empty,
                    PreparedAt = DateTime.UtcNow
                })
                .Then(context =>
                {
                    _logger.LogInformation(
                        "StateMachine: Both events received. Completing saga for OrderId: {OrderId}",
                        context.Saga.CorrelationId);
                    System.Console.WriteLine();
                    System.Console.WriteLine("=== Shipment Preparation Complete (State Machine) ===");
                    System.Console.WriteLine($"Order ID: {context.Saga.CorrelationId}");
                    System.Console.WriteLine("✓ Both OrderConfirmed and InventoryReserved received!");
                    System.Console.WriteLine("✓ ShipmentPrepared event published!");
                    System.Console.WriteLine();
                })
                .Finalize());

        // While waiting for order, accept OrderConfirmed
        During(WaitingForOrder,
            When(OrderConfirmed)
                .Then(context =>
                {
                    context.Saga.OrderConfirmed = true;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.ConfirmedAt = context.Message.ConfirmedAt;
                    _logger.LogInformation(
                        "StateMachine: OrderConfirmed received (was waiting) for OrderId: {OrderId}",
                        context.Message.OrderId);
                    LogSagaState(context);
                })
                .Publish(context => new ShipmentPrepared
                {
                    OrderId = context.Saga.CorrelationId,
                    CustomerId = context.Saga.CustomerId ?? Guid.Empty,
                    PreparedAt = DateTime.UtcNow
                })
                .Then(context =>
                {
                    _logger.LogInformation(
                        "StateMachine: Both events received. Completing saga for OrderId: {OrderId}",
                        context.Saga.CorrelationId);
                    System.Console.WriteLine();
                    System.Console.WriteLine("=== Shipment Preparation Complete (State Machine) ===");
                    System.Console.WriteLine($"Order ID: {context.Saga.CorrelationId}");
                    System.Console.WriteLine("✓ Both OrderConfirmed and InventoryReserved received!");
                    System.Console.WriteLine("✓ ShipmentPrepared event published!");
                    System.Console.WriteLine();
                })
                .Finalize());

        // Handle out-of-order messages (if OrderConfirmed arrives while in WaitingForOrder state)
        During(WaitingForInventory,
            When(OrderConfirmed)
                .Then(context =>
                {
                    // Already have OrderConfirmed, just update data if needed
                    _logger.LogInformation(
                        "StateMachine: OrderConfirmed received again (already confirmed) for OrderId: {OrderId}",
                        context.Message.OrderId);
                }));

        // Handle out-of-order messages (if InventoryReserved arrives while in WaitingForInventory state)
        During(WaitingForOrder,
            When(InventoryReserved)
                .Then(context =>
                {
                    // Already have InventoryReserved, just update data if needed
                    _logger.LogInformation(
                        "StateMachine: InventoryReserved received again (already reserved) for OrderId: {OrderId}",
                        context.Message.OrderId);
                }));
    }

    public State WaitingForInventory { get; private set; } = null!;
    public State WaitingForOrder { get; private set; } = null!;

    public Event<OrderConfirmed> OrderConfirmed { get; private set; } = null!;
    public Event<InventoryReserved> InventoryReserved { get; private set; } = null!;

    private void LogSagaState(BehaviorContext<ShipmentPreparationState> context)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("=== Shipment Preparation Saga (State Machine) ===");
        System.Console.WriteLine($"Order ID: {context.Saga.CorrelationId}");
        System.Console.WriteLine($"Current State: {context.Saga.CurrentState}");
        System.Console.WriteLine($"Order Confirmed: {(context.Saga.OrderConfirmed ? "✓" : "⏳")}");
        System.Console.WriteLine($"Inventory Reserved: {(context.Saga.InventoryReserved ? "✓" : "⏳")}");
        System.Console.WriteLine();
    }
}
