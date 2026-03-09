using MassTransit;
using MassTransitDemo.Features.Outbox.Data;

namespace MassTransitDemo.Features.Outbox.Handlers;

/// <summary>
/// Configures the receive endpoint for <see cref="CreateOrderHandler"/> following the
/// MassTransit transactional outbox documentation pattern:
/// https://masstransit.io/documentation/configuration/middleware/outbox
///
/// Two pieces of middleware are layered in the order that MassTransit recommends:
///   1. UseMessageRetry  — retries on transient failures before the outbox is involved.
///   2. UseEntityFrameworkOutbox — wraps the consumer pipeline so that ConsumeContext
///      publishes are stored in OutboxMessage and InboxState de-duplication is tracked,
///      all within the same PostgreSQL transaction as the consumer's DbContext writes.
/// </summary>
public sealed class CreateOrderConsumerDefinition : ConsumerDefinition<CreateOrderHandler>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CreateOrderHandler> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(100, 500, 1000));
        endpointConfigurator.UseEntityFrameworkOutbox<OutboxDbContext>(context);
    }
}
