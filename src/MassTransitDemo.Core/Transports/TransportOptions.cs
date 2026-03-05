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
    
    /// <summary>
    /// For Azure Service Bus: If true, use native dead-letter queue. If false, use custom named queues (_skipped/_error).
    /// </summary>
    public bool UseAzureServiceBusNativeDlq { get; init; } = true;

    /// <summary>
    /// If true, saga state is stored inside the transport's message session
    /// (<see cref="MassTransit.MessageSessionSagaRepository{TSaga}"/>).
    /// Requires a session-capable transport (Azure Service Bus).
    /// When false, an in-memory saga repository is used instead.
    /// </summary>
    public bool UseMessageSessionSagaRepository { get; init; }
}
