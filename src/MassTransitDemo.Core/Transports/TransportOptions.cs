namespace MassTransitDemo.Core.Transports;

/// <summary>
/// Configuration options for transport selection and connection.
/// </summary>
public sealed record TransportOptions
{
    public TransportType TransportType { get; init; } = TransportType.RabbitMQ;
    
    public string? AzureServiceBusConnectionString { get; init; }
    
    public string? RabbitMQConnectionString { get; init; }
    
    public string? PostgreSQLConnectionString { get; init; }
    
    public bool UseAzureServiceBusEmulator { get; init; } = true;
}
