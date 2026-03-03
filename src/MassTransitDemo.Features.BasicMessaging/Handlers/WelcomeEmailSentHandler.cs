using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.BasicMessaging.Handlers;

/// <summary>
/// Handler for WelcomeEmailSent events. Completes the handler chain.
/// </summary>
public sealed class WelcomeEmailSentHandler : IConsumer<WelcomeEmailSent>
{
    private readonly ILogger<WelcomeEmailSentHandler> _logger;

    public WelcomeEmailSentHandler(ILogger<WelcomeEmailSentHandler> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<WelcomeEmailSent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "WelcomeEmailSent event received - CustomerId: {CustomerId}, Email: {Email}",
            message.CustomerId,
            message.Email);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Welcome Email Sent ===");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Email: {message.Email}");
        System.Console.WriteLine($"Sent At: {message.SentAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();
        System.Console.WriteLine("✓ Handler chain completed successfully!");
        System.Console.WriteLine();

        return Task.CompletedTask;
    }
}
