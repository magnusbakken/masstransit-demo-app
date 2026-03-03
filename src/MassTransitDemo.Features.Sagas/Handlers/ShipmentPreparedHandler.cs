using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Sagas.Handlers;

/// <summary>
/// Handler for ShipmentPrepared event. Demonstrates that the saga completed successfully.
/// </summary>
public sealed class ShipmentPreparedHandler : IConsumer<ShipmentPrepared>
{
    private readonly ILogger<ShipmentPreparedHandler> _logger;

    public ShipmentPreparedHandler(ILogger<ShipmentPreparedHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ShipmentPrepared> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "ShipmentPrepared event received - OrderId: {OrderId}, CustomerId: {CustomerId}, PreparedAt: {PreparedAt}",
            message.OrderId,
            message.CustomerId,
            message.PreparedAt);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Shipment Prepared Event Received ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Prepared At: {message.PreparedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine("✓ Saga completed successfully!");
        System.Console.WriteLine();

        return Task.CompletedTask;
    }
}
