using MassTransit;

namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event indicating that an order has been confirmed. Can initiate a shipment preparation saga.
/// </summary>
public sealed record OrderConfirmed : CorrelatedBy<Guid>
{
    public Guid CorrelationId => OrderId;
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required DateTime ConfirmedAt { get; init; }
}
