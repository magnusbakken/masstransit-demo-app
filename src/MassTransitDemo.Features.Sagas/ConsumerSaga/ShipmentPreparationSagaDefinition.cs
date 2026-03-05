using MassTransit;

namespace MassTransitDemo.Features.Sagas.ConsumerSaga;

/// <summary>
/// Endpoint definition for <see cref="ShipmentPreparationSaga"/>.
/// Enables message session support on the saga's receive endpoint so that
/// <see cref="MessageSessionSagaRepository{TSaga}"/> can store saga state
/// inside the transport session.
/// </summary>
public sealed class ShipmentPreparationSagaDefinition : SagaDefinition<ShipmentPreparationSaga>
{
    private readonly SessionEndpointConfigurator _sessionConfigurator;

    public ShipmentPreparationSagaDefinition(SessionEndpointConfigurator sessionConfigurator)
    {
        _sessionConfigurator = sessionConfigurator;
    }

    protected override void ConfigureSaga(
        IReceiveEndpointConfigurator endpointConfigurator,
        ISagaConfigurator<ShipmentPreparationSaga> sagaConfigurator,
        IRegistrationContext context)
    {
        _sessionConfigurator.Configure(endpointConfigurator);
    }
}
