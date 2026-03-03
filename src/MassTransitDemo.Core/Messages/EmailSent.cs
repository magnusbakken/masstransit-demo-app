namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event published when an email has been sent.
/// </summary>
public sealed record EmailSent
{
    public required Guid CustomerId { get; init; }
    public required string Email { get; init; }
    public required string EmailType { get; init; }
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}
