using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.BasicMessaging.Handlers;

/// <summary>
/// Handler for SendVerificationEmail commands. Sends verification email and publishes EmailSent event.
/// </summary>
public sealed class SendVerificationEmailHandler : IConsumer<SendVerificationEmail>
{
    private readonly ILogger<SendVerificationEmailHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public SendVerificationEmailHandler(
        ILogger<SendVerificationEmailHandler> logger,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<SendVerificationEmail> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Sending verification email to CustomerId: {CustomerId}, Email: {Email}",
            message.CustomerId,
            message.Email);

        // Simulate sending verification email
        await Task.Delay(100); // Simulate email sending delay

        System.Console.WriteLine();
        System.Console.WriteLine("=== Verification Email Sent ===");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Email: {message.Email}");
        System.Console.WriteLine();

        // Publish EmailSent event to continue the chain
        await _publishEndpoint.Publish(new EmailSent
        {
            CustomerId = message.CustomerId,
            Email = message.Email,
            EmailType = "Verification"
        });

        _logger.LogInformation("EmailSent event published for CustomerId: {CustomerId}", message.CustomerId);
    }
}
