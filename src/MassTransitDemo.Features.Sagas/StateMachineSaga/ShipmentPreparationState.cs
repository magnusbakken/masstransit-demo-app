using MassTransit;

namespace MassTransitDemo.Features.Sagas.StateMachineSaga;

/// <summary>
/// State for the state machine-based shipment preparation saga.
/// </summary>
public sealed class ShipmentPreparationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public bool OrderConfirmed { get; set; }
    public bool InventoryReserved { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ReservedAt { get; set; }
}
