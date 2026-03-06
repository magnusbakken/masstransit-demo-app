using MassTransit;
using MassTransitDemo.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.BasicMessaging.Handlers;

/// <summary>
/// Handler for the CustomerCreated event (pub/sub — arrives via topic fan-out).
/// Sends the SendVerificationEmail command point-to-point to its dedicated queue,
/// which starts the handler chain.
/// </summary>
public sealed class CustomerCreatedHandler : IConsumer<CustomerCreated>
{
    private readonly ILogger<CustomerCreatedHandler> _logger;
    private readonly IEndpointNameFormatter _endpointNameFormatter;

    public CustomerCreatedHandler(
        ILogger<CustomerCreatedHandler> logger,
        IEndpointNameFormatter endpointNameFormatter)
    {
        _logger = logger;
        _endpointNameFormatter = endpointNameFormatter;
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

        // Send the command point-to-point to the SendVerificationEmail handler's queue.
        // Using Send (not Publish) because SendVerificationEmail is a command — it has
        // exactly one intended receiver, so point-to-point delivery is correct.
        var address = new Uri($"queue:{_endpointNameFormatter.Consumer<SendVerificationEmailHandler>()}");
        var endpoint = await context.GetSendEndpoint(address);
        await endpoint.Send(new SendVerificationEmail
        {
            CustomerId = message.CustomerId,
            Email = message.Email,
            CustomerName = message.CustomerName
        });

        _logger.LogInformation("SendVerificationEmail command sent for CustomerId: {CustomerId}", message.CustomerId);
    }
}
