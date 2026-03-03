using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.ErrorHandling.Handlers;

/// <summary>
/// Handler for ProcessOrder commands that fails on the first attempt but succeeds on retry.
/// Demonstrates retry mechanism with exponential backoff.
/// </summary>
public sealed class ProcessOrderHandler : IConsumer<ProcessOrder>
{
    private readonly ILogger<ProcessOrderHandler> _logger;
    private static readonly Dictionary<Guid, int> AttemptCounts = new();

    public ProcessOrderHandler(ILogger<ProcessOrderHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ProcessOrder> context)
    {
        var message = context.Message;
        
        // Track attempt count per order
        if (!AttemptCounts.ContainsKey(message.OrderId))
        {
            AttemptCounts[message.OrderId] = 0;
        }
        
        AttemptCounts[message.OrderId]++;

        var attemptCount = AttemptCounts[message.OrderId];

        _logger.LogInformation(
            "Processing order OrderId: {OrderId}, CustomerId: {CustomerId}, Attempt: {Attempt}",
            message.OrderId,
            message.CustomerId,
            attemptCount);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Processing Order ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Total Amount: {message.TotalAmount:C}");
        System.Console.WriteLine($"Attempt: {attemptCount}");
        System.Console.WriteLine();

        // Fail on first attempt, succeed on second
        if (attemptCount == 1)
        {
            _logger.LogWarning("Order processing failed on first attempt (intentional for retry demo)");
            throw new InvalidOperationException(
                $"Order processing failed on first attempt for OrderId: {message.OrderId}. " +
                "This will be retried automatically.");
        }

        // Success on second attempt
        _logger.LogInformation("Order processed successfully on attempt {Attempt}", attemptCount);
        System.Console.WriteLine("✓ Order processed successfully!");
        System.Console.WriteLine();

        // Clean up attempt count after successful processing
        AttemptCounts.Remove(message.OrderId);

        return Task.CompletedTask;
    }
}
