using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Outbox.Handlers;

/// <summary>
/// Handler for OrderCreated event. Demonstrates that the event was successfully published
/// after the database transaction committed.
/// </summary>
public sealed class OrderCreatedHandler : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "OrderCreated event received - OrderId: {OrderId}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount);

        System.Console.WriteLine();
        System.Console.WriteLine("=== OrderCreated Event Received ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Total Amount: {message.TotalAmount:C}");
        System.Console.WriteLine($"Created At: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine("✓ Event successfully delivered from outbox!");
        System.Console.WriteLine();

        return Task.CompletedTask;
    }
}
