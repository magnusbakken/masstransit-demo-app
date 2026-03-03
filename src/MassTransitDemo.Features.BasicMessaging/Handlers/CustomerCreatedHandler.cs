using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.BasicMessaging.Handlers;

/// <summary>
/// Handler for CustomerCreated events. 
/// For basic messaging demo: Simply prints output.
/// For handler chain demo: Publishes SendVerificationEmail command to trigger the chain.
/// </summary>
public sealed class CustomerCreatedHandler : IConsumer<CustomerCreated>
{
    private readonly ILogger<CustomerCreatedHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public CustomerCreatedHandler(
        ILogger<CustomerCreatedHandler> logger,
        IPublishEndpoint publishEndpoint)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<CustomerCreated> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "CustomerCreated event received - CustomerId: {CustomerId}, Name: {CustomerName}, Email: {Email}, CreatedAt: {CreatedAt}",
            message.CustomerId,
            message.CustomerName,
            message.Email,
            message.CreatedAt);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Customer Created ===");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Name: {message.CustomerName}");
        System.Console.WriteLine($"Email: {message.Email}");
        System.Console.WriteLine($"Created At: {message.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        System.Console.WriteLine();

        // For handler chain demo: Publish SendVerificationEmail command to continue the chain
        // This will be triggered when CustomerCreated is published as part of the chain demo
        // Note: In a production scenario, you might use a flag or separate event types
        // to distinguish between basic messaging and chain scenarios
        await _publishEndpoint.Publish(new SendVerificationEmail
        {
            CustomerId = message.CustomerId,
            Email = message.Email,
            CustomerName = message.CustomerName
        });

        _logger.LogInformation("SendVerificationEmail command published for CustomerId: {CustomerId}", message.CustomerId);
    }
}
