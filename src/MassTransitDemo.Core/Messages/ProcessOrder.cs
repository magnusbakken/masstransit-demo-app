namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Command to process an order. Used to demonstrate retry mechanism.
/// </summary>
public sealed record ProcessOrder
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public int AttemptCount { get; init; } = 0;
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
