namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Command to create an order. Used to demonstrate transactional outbox pattern.
/// </summary>
public sealed record CreateOrder
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required List<OrderItem> Items { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an item in an order.
/// </summary>
public sealed record OrderItem
{
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
}
