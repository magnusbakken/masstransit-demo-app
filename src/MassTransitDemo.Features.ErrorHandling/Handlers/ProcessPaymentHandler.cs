using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.ErrorHandling.Handlers;

/// <summary>
/// Handler for ProcessPayment commands that intentionally always throws an exception
/// to demonstrate dead-letter queue handling.
/// </summary>
public sealed class ProcessPaymentHandler : IConsumer<ProcessPayment>
{
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(ILogger<ProcessPaymentHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;
        _logger.LogError(
            "Processing payment PaymentId: {PaymentId}, OrderId: {OrderId}, Amount: {Amount}",
            message.PaymentId,
            message.OrderId,
            message.Amount);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Processing Payment (Will Fail) ===");
        System.Console.WriteLine($"Payment ID: {message.PaymentId}");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Amount: {message.Amount:C}");
        System.Console.WriteLine();

        // Intentionally throw an exception to demonstrate error handling
        throw new InvalidOperationException(
            $"Payment processing failed for PaymentId: {message.PaymentId}. " +
            "This is intentional to demonstrate dead-letter queue handling.");
    }
}
