using MassTransit;
using MassTransitDemo.Core.Transports;

namespace MassTransitDemo.Transports;

/// <summary>
/// Configures MassTransit to use RabbitMQ transport.
/// </summary>
public sealed class RabbitMqTransportConfigurator : IBusTransportConfigurator
{
    private readonly TransportOptions _options;

    public RabbitMqTransportConfigurator(TransportOptions options)
    {
        _options = options;
    }

    public void Configure(IBusRegistrationConfigurator configurator)
    {
        configurator.UsingRabbitMq((context, cfg) =>
        {
            if (!string.IsNullOrWhiteSpace(_options.RabbitMQConnectionString))
            {
                // Use connection string if provided
                cfg.Host(_options.RabbitMQConnectionString);
            }
            else
            {
                // Default to Docker Compose configuration
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("masstransit");
                    h.Password("masstransit");
                });
            }

            cfg.ConfigureEndpoints(context);
        });
    }
}
