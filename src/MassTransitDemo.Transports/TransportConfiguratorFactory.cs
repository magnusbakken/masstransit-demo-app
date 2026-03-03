using MassTransitDemo.Core.Transports;

namespace MassTransitDemo.Transports;

/// <summary>
/// Factory for creating transport configurators based on transport type.
/// </summary>
public static class TransportConfiguratorFactory
{
    public static IBusTransportConfigurator Create(TransportOptions options)
    {
        return options.TransportType switch
        {
            TransportType.AzureServiceBus => new AzureServiceBusTransportConfigurator(options),
            TransportType.RabbitMQ => new RabbitMqTransportConfigurator(options),
            TransportType.PostgreSQL => new PostgreSqlTransportConfigurator(options),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options.TransportType),
                options.TransportType,
                "Unsupported transport type")
        };
    }
}
