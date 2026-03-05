using MassTransit;

namespace MassTransitDemo.Features.Sagas.StateMachineSaga;

/// <summary>
/// Endpoint definition for <see cref="ShipmentPreparationStateMachine"/>.
/// Enables message session support on the saga's receive endpoint so that
/// <see cref="MessageSessionSagaRepository{TSaga}"/> can store saga state
/// inside the transport session.
/// </summary>
public sealed class ShipmentPreparationStateMachineDefinition : SagaDefinition<ShipmentPreparationState>
{
    private readonly SessionEndpointConfigurator _sessionConfigurator;

    public ShipmentPreparationStateMachineDefinition(SessionEndpointConfigurator sessionConfigurator)
    {
        _sessionConfigurator = sessionConfigurator;
    }

    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<ShipmentPreparationState> sagaConfigurator,
        IRegistrationContext context)
    {
        _sessionConfigurator.Configure(endpointConfigurator);
    }
}
