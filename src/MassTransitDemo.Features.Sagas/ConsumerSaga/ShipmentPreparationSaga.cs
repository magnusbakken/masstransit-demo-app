using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Sagas.ConsumerSaga;

/// <summary>
/// Consumer-based saga for shipment preparation.
/// Can be initiated by either OrderConfirmed or InventoryReserved messages.
/// Completes when both messages have been received.
/// </summary>
public sealed class ShipmentPreparationSaga :
    ISaga,
    InitiatedBy<OrderConfirmed>,
    InitiatedBy<InventoryReserved>,
    Orchestrates<OrderConfirmed>,
    Orchestrates<InventoryReserved>
{
    public Guid CorrelationId { get; set; }

    public bool OrderConfirmed { get; set; }
    public bool InventoryReserved { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReservedAt { get; set; }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var message = context.Message;
        CorrelationId = message.OrderId;

        if (context.TryGetPayload<ILogger<ShipmentPreparationSaga>>(out var logger))
        {
            logger.LogInformation(
                "ShipmentPreparationSaga: OrderConfirmed received for OrderId: {OrderId}",
                message.OrderId);
        }

        OrderConfirmed = true;
        CustomerId = message.CustomerId;
        ConfirmedAt = message.ConfirmedAt;

        System.Console.WriteLine();
        System.Console.WriteLine("=== Shipment Preparation Saga (Consumer) ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Order Confirmed: ✓");
        System.Console.WriteLine($"Inventory Reserved: {(InventoryReserved ? "✓" : "⏳")}");
        System.Console.WriteLine();

        await CheckAndCompleteAsync(context);
    }

    public async Task Consume(ConsumeContext<InventoryReserved> context)
    {
        var message = context.Message;
        CorrelationId = message.OrderId;

        if (context.TryGetPayload<ILogger<ShipmentPreparationSaga>>(out var logger))
        {
            logger.LogInformation(
                "ShipmentPreparationSaga: InventoryReserved received for OrderId: {OrderId}",
                message.OrderId);
        }

        InventoryReserved = true;
        ReservedAt = message.ReservedAt;

        System.Console.WriteLine();
        System.Console.WriteLine("=== Shipment Preparation Saga (Consumer) ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Order Confirmed: {(OrderConfirmed ? "✓" : "⏳")}");
        System.Console.WriteLine($"Inventory Reserved: ✓");
        System.Console.WriteLine();

        await CheckAndCompleteAsync(context);
    }

    private async Task CheckAndCompleteAsync(ConsumeContext context)
    {
        if (OrderConfirmed && InventoryReserved)
        {
            if (context.TryGetPayload<ILogger<ShipmentPreparationSaga>>(out var logger))
            {
                logger.LogInformation(
                    "ShipmentPreparationSaga: Both events received. Completing saga for OrderId: {OrderId}",
                    CorrelationId);
            }

            await context.Publish(new ShipmentPrepared
            {
                OrderId = CorrelationId,
                CustomerId = CustomerId ?? Guid.Empty,
                PreparedAt = DateTime.UtcNow
            });

            System.Console.WriteLine();
            System.Console.WriteLine("=== Shipment Preparation Complete (Consumer Saga) ===");
            System.Console.WriteLine($"Order ID: {CorrelationId}");
            System.Console.WriteLine("✓ Both OrderConfirmed and InventoryReserved received!");
            System.Console.WriteLine("✓ ShipmentPrepared event published!");
            System.Console.WriteLine();
        }
    }
}
