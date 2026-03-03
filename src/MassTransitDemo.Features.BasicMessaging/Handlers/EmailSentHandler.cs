using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.BasicMessaging.Handlers;

/// <summary>
/// Handler for EmailSent events. Publishes WelcomeEmailSent event to continue the chain.
/// </summary>
public sealed class EmailSentHandler : IConsumer<EmailSent>
{
    private readonly ILogger<EmailSentHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public EmailSentHandler(
        ILogger<EmailSentHandler> logger,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<EmailSent> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "EmailSent event received - CustomerId: {CustomerId}, EmailType: {EmailType}",
            message.CustomerId,
            message.EmailType);

        // Only send welcome email if this was a verification email
        if (message.EmailType == "Verification")
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=== Processing EmailSent Event ===");
            System.Console.WriteLine($"Customer ID: {message.CustomerId}");
            System.Console.WriteLine($"Email Type: {message.EmailType}");
            System.Console.WriteLine("Triggering welcome email...");
            System.Console.WriteLine();

            // Publish WelcomeEmailSent event to complete the chain
            await _publishEndpoint.Publish(new WelcomeEmailSent
            {
                CustomerId = message.CustomerId,
                Email = message.Email
            });

            _logger.LogInformation("WelcomeEmailSent event published for CustomerId: {CustomerId}", message.CustomerId);
        }
    }
}
