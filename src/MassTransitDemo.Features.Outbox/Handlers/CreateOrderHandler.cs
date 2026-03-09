using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Outbox.Data;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Features.Outbox.Handlers;

/// <summary>
/// Handles the CreateOrder command using the MassTransit transactional outbox pattern.
///
/// The EF Core outbox middleware (configured via CreateOrderConsumerDefinition) wraps this
/// consumer's receive pipeline so that:
///   1. Inbound message de-duplication is tracked in the InboxState table.
///   2. Any message published via ConsumeContext is buffered into the OutboxMessage table
///      rather than sent directly to the broker.
///   3. SaveChangesAsync commits both the business row (Orders) and the outbox row
///      (OutboxMessage) in a single PostgreSQL transaction — atomically.
///   4. The background OutboxDeliveryService reads pending OutboxMessage rows and
///      forwards them to the broker, guaranteeing at-least-once delivery even if the
///      process crashes between the commit and the broker send.
/// </summary>
public sealed class CreateOrderHandler : IConsumer<CreateOrder>
{
    private readonly OutboxDbContext _dbContext;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(OutboxDbContext dbContext, ILogger<CreateOrderHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateOrder> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Creating order {OrderId} for customer {CustomerId}, amount {TotalAmount}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount);

        _dbContext.Orders.Add(new Order
        {
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt
        });

        // Publishing via ConsumeContext — the outbox middleware intercepts this call and
        // writes the serialised message into the OutboxMessage table instead of sending
        // it to the broker immediately.
        await context.Publish(new OrderCreated
        {
            OrderId = message.OrderId,
            CustomerId = message.CustomerId,
            TotalAmount = message.TotalAmount,
            CreatedAt = message.CreatedAt
        });

        // A single SaveChangesAsync commits both the new Orders row and the new
        // OutboxMessage row within the same PostgreSQL transaction.  If this call throws,
        // neither the business record nor the outbox entry is persisted — the message
        // will be retried and no phantom event will ever reach the broker.
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Order {OrderId} saved; OrderCreated staged in outbox — delivery pending",
            message.OrderId);

        System.Console.WriteLine();
        System.Console.WriteLine("=== Order Created (Transactional Outbox) ===");
        System.Console.WriteLine($"Order ID:    {message.OrderId}");
        System.Console.WriteLine($"Customer ID: {message.CustomerId}");
        System.Console.WriteLine($"Amount:      {message.TotalAmount:C}");
        System.Console.WriteLine($"Items:       {message.Items.Count}");
        System.Console.WriteLine();
        System.Console.WriteLine("Orders row + OutboxMessage row committed atomically.");
        System.Console.WriteLine("OutboxDeliveryService will forward OrderCreated to the broker.");
        System.Console.WriteLine();
    }
}
