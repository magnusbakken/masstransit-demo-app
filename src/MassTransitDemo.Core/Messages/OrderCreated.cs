namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event published when an order is created. Used to demonstrate transactional outbox pattern.
/// </summary>
public sealed record OrderCreated
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required DateTime CreatedAt { get; init; }
}
