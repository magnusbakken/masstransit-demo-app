using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Outbox.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Outbox.Handlers;

/// <summary>
/// Handler for CreateOrder command that demonstrates transactional outbox.
/// Updates the database and publishes an event atomically.
/// </summary>
public sealed class CreateOrderHandler : IConsumer<CreateOrder>
{
    private readonly OutboxDbContext _dbContext;
    private readonly ILogger<CreateOrderHandler> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateOrderHandler(
        OutboxDbContext dbContext,
        ILogger<CreateOrderHandler> logger,
        IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<CreateOrder> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Creating order OrderId: {OrderId}, CustomerId: {CustomerId}, TotalAmount: {TotalAmount}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount);

        // Create order entity
        var order = new Order
        {
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt
        };

        // Add to database
        _dbContext.Orders.Add(order);

        // Publish event - this will be stored in outbox and delivered atomically with SaveChanges
        await _publishEndpoint.Publish(new OrderCreated
        {
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt
        });

        // Save changes - this commits both the order and the outbox message in a single transaction
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order created and OrderCreated event published atomically - OrderId: {OrderId}", message.OrderId);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Order Created (Transactional Outbox) ===");
        System.Console.WriteLine($"Order ID: {message.OrderId}");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Total Amount: {message.TotalAmount:C}");
        System.Console.WriteLine($"Items: {message.Items.Count}");
        System.Console.WriteLine("✓ Database update and event publish committed atomically!");
        System.Console.WriteLine();
    }
}
