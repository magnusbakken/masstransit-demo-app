namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event published when a welcome email has been sent to a customer.
/// </summary>
public sealed record WelcomeEmailSent
{
    public required Guid CustomerId { get; init; }
    public required string Email { get; init; }
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}
