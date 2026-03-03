namespace MassTransitDemo.Core.Transports;

/// <summary>
/// Supported transport types for the MassTransit demo application.
/// </summary>
public enum TransportType
{
    AzureServiceBus,
    RabbitMQ,
    PostgreSQL
}
