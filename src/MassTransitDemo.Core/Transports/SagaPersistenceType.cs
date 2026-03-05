namespace MassTransitDemo.Core.Transports;

/// <summary>
/// Supported saga persistence strategies.
/// </summary>
public enum SagaPersistenceType
{
    /// <summary>
    /// Saga state is kept in process memory (lost on restart).
    /// </summary>
    InMemory,

    /// <summary>
    /// Saga state is stored inside the transport's message session.
    /// Requires a session-capable transport (Azure Service Bus).
    /// </summary>
    MessageSession,

    /// <summary>
    /// Saga state is persisted via Entity Framework Core (PostgreSQL).
    /// </summary>
    EntityFramework
}
