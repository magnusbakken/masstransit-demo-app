using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.TopicFanout.Handlers;

/// <summary>
/// One of two consumers for the OrderShipped event.
/// Demonstrates pub/sub fan-out: both this handler and ShippingNotificationHandler
/// receive the same published event concurrently, each on its own queue.
/// 
/// Transport topology:
///   RabbitMQ         — fanout exchange "OrderShipped" → queue for this consumer
///   PostgreSQL       — topic "OrderShipped" → subscription for this consumer
///   Azure Svc Bus    — topic "OrderShipped" → subscription for this consumer
/// </summary>
public sealed class WarehouseUpdateHandler : IConsumer<OrderShipped>
{
    private readonly ILogger<WarehouseUpdateHandler> _logger;

    public WarehouseUpdateHandler(ILogger<WarehouseUpdateHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderShipped> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "WarehouseUpdateHandler: updating records for order {OrderId} " +
            "(customer {CustomerId}) shipped at {ShippedAt}",
            message.OrderId,
            message.CustomerId,
            message.ShippedAt);

        System.Console.WriteLine();
        System.Console.WriteLine("=== [WarehouseUpdateHandler] Warehouse Records Updated ===");
        System.Console.WriteLine($"  Order ID:        {message.OrderId}");
        System.Console.WriteLine($"  Customer ID:     {message.CustomerId}");
        System.Console.WriteLine($"  Tracking Number: {message.TrackingNumber}");
        System.Console.WriteLine($"  Shipped At:      {message.ShippedAt:yyyy-MM-dd HH:mm:ss zzz}");
        System.Console.WriteLine("  → Outbound shipment record marked as dispatched.");
        System.Console.WriteLine();

        return Task.CompletedTask;
    }
}
