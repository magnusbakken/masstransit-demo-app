namespace MassTransitDemo.Core.Messages;

public sealed record OrderShipped
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public DateTimeOffset ShippedAt { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
}
