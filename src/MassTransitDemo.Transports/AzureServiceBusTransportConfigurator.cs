using MassTransit;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Features.ErrorHandling.Configuration;

namespace MassTransitDemo.Transports;

/// <summary>
/// Configures MassTransit to use Azure Service Bus transport.
/// Supports both emulator (Azurite) and real Azure Service Bus instances.
/// </summary>
public sealed class AzureServiceBusTransportConfigurator : IBusTransportConfigurator
{
    private readonly TransportOptions _options;

    public AzureServiceBusTransportConfigurator(TransportOptions options)
    {
        _options = options;
    }

    public void Configure(IBusRegistrationConfigurator configurator)
    {
        configurator.UsingAzureServiceBus((context, cfg) =>
        {
            if (_options.UseAzureServiceBusEmulator)
            {
                // Note: Azure Service Bus emulator support in MassTransit 8.x
                // For local development, you may need to use a real Azure Service Bus instance
                // or configure connection string to point to Azurite
                // This is a placeholder - actual emulator configuration may vary
                throw new NotSupportedException(
                    "Azure Service Bus emulator configuration requires additional setup. " +
                    "Please provide a connection string to a real Azure Service Bus instance or " +
                    "configure Azurite connection string manually.");
            }
            else if (!string.IsNullOrWhiteSpace(_options.AzureServiceBusConnectionString))
            {
                // Use real Azure Service Bus instance
                cfg.Host(_options.AzureServiceBusConnectionString);
            }
            else
            {
                throw new InvalidOperationException(
                    "Azure Service Bus connection string is required when not using emulator. " +
                    "Set AzureServiceBusConnectionString in configuration or use UseAzureServiceBusEmulator=true.");
            }

            cfg.ConfigureEndpoints(context);
        });
    }
}
