namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Command to process a payment. Used to demonstrate error handling and dead-letter queues.
/// </summary>
public sealed record ProcessPayment
{
    public required Guid PaymentId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string PaymentMethod { get; init; }
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
}
