using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.TopicFanout.Handlers;

/// <summary>
/// One of two consumers for the OrderShipped event.
/// Demonstrates pub/sub fan-out: both this handler and WarehouseUpdateHandler
/// receive the same published event concurrently, each on its own queue.
/// 
/// Transport topology:
///   RabbitMQ         — fanout exchange "OrderShipped" → queue for this consumer
///   PostgreSQL       — topic "OrderShipped" → subscription for this consumer
///   Azure Svc Bus    — topic "OrderShipped" → subscription for this consumer
/// </summary>
public sealed class ShippingNotificationHandler : IConsumer<OrderShipped>
{
    private readonly ILogger<ShippingNotificationHandler> _logger;

    public ShippingNotificationHandler(ILogger<ShippingNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderShipped> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "ShippingNotificationHandler: notifying customer {CustomerId} — " +
            "Order {OrderId} shipped with tracking {TrackingNumber} at {ShippedAt}",
            message.CustomerId,
            message.OrderId,
            message.TrackingNumber,
            message.ShippedAt);

        System.Console.WriteLine();
        System.Console.WriteLine("=== [ShippingNotificationHandler] Customer Notified ===");
        System.Console.WriteLine($"  Order ID:        {message.OrderId}");
        System.Console.WriteLine($"  Customer ID:     {message.CustomerId}");
        System.Console.WriteLine($"  Tracking Number: {message.TrackingNumber}");
        System.Console.WriteLine($"  Shipped At:      {message.ShippedAt:yyyy-MM-dd HH:mm:ss zzz}");
        System.Console.WriteLine("  → Shipment confirmation email sent to customer.");
        System.Console.WriteLine();

        return Task.CompletedTask;
    }
}
