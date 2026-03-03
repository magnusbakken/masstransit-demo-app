namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event published when a new customer is created.
/// </summary>
public sealed record CustomerCreated
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required string Email { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
