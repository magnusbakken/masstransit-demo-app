namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Command to send a verification email to a customer.
/// </summary>
public sealed record SendVerificationEmail
{
    public required Guid CustomerId { get; init; }
    public required string Email { get; init; }
    public required string CustomerName { get; init; }
}
