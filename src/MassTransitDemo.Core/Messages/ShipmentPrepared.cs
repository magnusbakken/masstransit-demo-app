namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event published when shipment preparation is complete (both OrderConfirmed and InventoryReserved received).
/// </summary>
public sealed record ShipmentPrepared
{
    public required Guid OrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public required DateTime PreparedAt { get; init; }
}
