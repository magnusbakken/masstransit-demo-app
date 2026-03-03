using MassTransit;

namespace MassTransitDemo.Core.Transports;

/// <summary>
/// Abstraction for configuring MassTransit transports.
/// </summary>
public interface IBusTransportConfigurator
{
    /// <summary>
    /// Configures the MassTransit bus factory with the specific transport.
    /// </summary>
    /// <param name="configurator">The bus factory configurator.</param>
    void Configure(IBusRegistrationConfigurator configurator);
}
