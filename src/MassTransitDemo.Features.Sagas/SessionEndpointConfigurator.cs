using MassTransit;

namespace MassTransitDemo.Features.Sagas;

/// <summary>
/// Configures receive endpoints for message session support.
/// Transport-specific session setup (e.g. Azure Service Bus RequireSession)
/// is injected via the constructor delegate, keeping saga definitions
/// decoupled from any particular transport.
/// </summary>
public sealed class SessionEndpointConfigurator
{
    public static SessionEndpointConfigurator NoOp { get; } = new(null);

    private readonly Action<IReceiveEndpointConfigurator>? _configure;

    public SessionEndpointConfigurator(Action<IReceiveEndpointConfigurator>? configure)
    {
        _configure = configure;
    }

    public void Configure(IReceiveEndpointConfigurator endpointConfigurator)
    {
        _configure?.Invoke(endpointConfigurator);
    }
}
