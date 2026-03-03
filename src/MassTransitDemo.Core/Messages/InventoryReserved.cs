using MassTransit;

namespace MassTransitDemo.Core.Messages;

/// <summary>
/// Event indicating that inventory has been reserved. Can initiate a shipment preparation saga.
/// </summary>
public sealed record InventoryReserved : CorrelatedBy<Guid>
{
    public Guid CorrelationId => OrderId;
    public required Guid OrderId { get; init; }
    public required List<ReservedItem> Items { get; init; }
    public required DateTime ReservedAt { get; init; }
}

/// <summary>
/// Represents a reserved inventory item.
/// </summary>
public sealed record ReservedItem
{
    public required string ProductId { get; init; }
    public required int Quantity { get; init; }
}
