using MassTransit;
using MassTransitDemo.Core.Transports;

namespace MassTransitDemo.Transports;

/// <summary>
/// Configures MassTransit to use PostgreSQL transport.
/// Note: SqlTransportOptions should be configured via dependency injection in the host builder.
/// </summary>
public sealed class PostgreSqlTransportConfigurator : IBusTransportConfigurator
{
    private readonly TransportOptions _options;

    public PostgreSqlTransportConfigurator(TransportOptions options)
    {
        _options = options;
    }

    public void Configure(IBusRegistrationConfigurator configurator)
    {
        configurator.UsingPostgres((context, cfg) =>
        {
            // SqlTransportOptions are configured via DI in Program.cs
            cfg.ConfigureEndpoints(context);
        });
    }
}
