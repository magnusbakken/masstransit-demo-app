# MassTransit Demo Application

A comprehensive demo application showcasing MassTransit 8.x messaging patterns with .NET 10. Each use case is implemented as an isolated feature module and can be triggered from an interactive console menu or via CLI arguments.

## Use Cases

### 1. Basic Messaging

Publishes a `CustomerCreated` event and consumes it with a single handler. Demonstrates the fundamental MassTransit publish/subscribe pattern where an event is broadcast and any subscribed consumer receives a copy.

**Message flow:**

```
[Publish] CustomerCreated ──► CustomerCreatedHandler
```

`CustomerCreatedHandler` logs the new customer details and sends a `SendVerificationEmail` command to the verification handler's queue, which ties into the handler chain (use case 2).

### 2. Handler Chain

Builds on basic messaging to demonstrate a multi-step message pipeline that mixes events (fan-out) with commands (point-to-point).

**Message flow:**

```
[Publish] CustomerCreated
    └──► CustomerCreatedHandler
            └──[Send] SendVerificationEmail
                    └──► SendVerificationEmailHandler
                            └──[Publish] EmailSent
                                    └──► EmailSentHandler (filters EmailType == "Verification")
                                            └──[Publish] WelcomeEmailSent
                                                    └──► WelcomeEmailSentHandler
```

Each step in the chain uses the appropriate messaging semantic: `Send` for commands addressed to a specific queue, `Publish` for events that fan out to all subscribers.

### 3. Error Handling (Dead-Letter Queue)

Sends a `ProcessPayment` command to a handler that always throws an `InvalidOperationException`. The consumer is configured with `UseMessageRetry(r => r.None())` so no retries are attempted. After failure the message is moved to the transport's dead-letter queue.

**Message flow:**

```
[Send] ProcessPayment ──► ProcessPaymentHandler ──✗ throws ──► Dead-Letter Queue
```

When Azure Service Bus is selected and the `UseAzureServiceBusNativeDlq` option is enabled, the application configures `ConfigureDeadLetterQueueDeadLetterTransport()` and `ConfigureDeadLetterQueueErrorTransport()` so failed messages land in the ASB native DLQ rather than the MassTransit error queue.

### 4. Retry Mechanism

Sends a `ProcessOrder` command to a handler that deliberately fails on the first attempt and succeeds on the second. The consumer is configured with an exponential back-off retry policy (5 retries, 1 s initial interval, 10 s max interval, 2 s increment).

**Message flow:**

```
[Send] ProcessOrder ──► ProcessOrderHandler
                           ├── Attempt 1: throws ──► retry (exponential back-off)
                           └── Attempt 2: succeeds ✓
```

Attempt tracking uses a `ConcurrentDictionary<Guid, int>` keyed on `OrderId`, so each unique order fails exactly once.

### 5. Transactional Outbox

Sends a `CreateOrder` command whose handler persists an `Order` entity to PostgreSQL and publishes an `OrderCreated` event inside the same database transaction. MassTransit's Entity Framework Core outbox guarantees that the database write and the event publish are committed atomically — if the transaction rolls back, the event is never delivered.

**Message flow:**

```
[Send] CreateOrder ──► CreateOrderHandler
                         ├── Insert Order row
                         ├── Publish OrderCreated (stored in outbox table)
                         └── SaveChangesAsync() ── single transaction
                                                        │
                         Outbox delivery service ◄──────┘
                                │
                         [Deliver] OrderCreated ──► OrderCreatedHandler
```

The outbox is configured with `AddEntityFrameworkOutbox<OutboxDbContext>` using the PostgreSQL provider and the bus outbox (`UseBusOutbox()`). `OutboxDbContext` contains the `Order` entity plus the MassTransit inbox/outbox state tables.

### 6. Consumer Saga

Implements a shipment-preparation workflow using a MassTransit consumer saga (`ISaga`). The saga is initiated by either an `OrderConfirmed` or an `InventoryReserved` message (order-independent) and completes when both have been received for the same `OrderId`. On completion it publishes a `ShipmentPrepared` event.

**Message flow:**

```
[Send] OrderConfirmed ─────┐
                           ├──► ShipmentPreparationSaga
[Send] InventoryReserved ──┘        │
                                    ▼ (both received)
                             [Publish] ShipmentPrepared ──► ShipmentPreparedHandler
```

The saga correlates messages by `OrderId` and uses `InitiatedByOrOrchestrates<T>` so either message can create or update the saga instance. The interactive menu and CLI let you choose the event arrival order (order-first, inventory-first, or concurrent).

### 7. State Machine Saga

Implements the same shipment-preparation workflow as use case 6, but using a `MassTransitStateMachine<ShipmentPreparationState>` instead of a consumer saga.

**States:** `Initial` → `WaitingForInventory` / `WaitingForOrder` → `Final`

**Transitions:**

| Current State | Event | Next State |
|---|---|---|
| `Initial` | `OrderConfirmed` | `WaitingForInventory` |
| `Initial` | `InventoryReserved` | `WaitingForOrder` |
| `WaitingForInventory` | `InventoryReserved` | `Final` (publishes `ShipmentPrepared`) |
| `WaitingForOrder` | `OrderConfirmed` | `Final` (publishes `ShipmentPrepared`) |

Duplicate events in the same state are handled gracefully (logged but ignored).

### 8. Topic Fan-out

Publishes an `OrderShipped` event that is consumed by two independent handlers, each on its own queue. Demonstrates how a single published event fans out to multiple subscribers through transport-specific topology.

**Message flow:**

```
[Publish] OrderShipped ──┬──► ShippingNotificationHandler (notifies customer)
                         └──► WarehouseUpdateHandler      (updates warehouse records)
```

**Transport topology:**

- **RabbitMQ** — fanout exchange bound to two separate queues
- **PostgreSQL** — topic with two independent subscriptions
- **Azure Service Bus** — topic with two independent subscriptions

### Saga Persistence Options

Both sagas (consumer and state machine) support three persistence strategies, selectable via `appsettings.json` or the `--saga-persistence` CLI option:

| Strategy | Description |
|---|---|
| `InMemory` | In-process dictionary (default, no infrastructure needed) |
| `MessageSession` | Azure Service Bus message sessions (`RequiresSession = true`) |
| `EntityFramework` | PostgreSQL via `SagaDbContext` with pessimistic concurrency |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (10.0.100+, `rollForward: patch` per `global.json`)
- [Docker](https://www.docker.com/) and Docker Compose

## Setup

### Start Infrastructure Services

```bash
docker compose up -d postgres rabbitmq
```

| Service | Ports | Credentials |
|---|---|---|
| **PostgreSQL** | 5432 | `masstransit` / `masstransit` (database: `masstransit_demo`) |
| **RabbitMQ** | 5672 (AMQP), 15672 (Management UI) | `masstransit` / `masstransit` |

Optional services defined in `docker-compose.yml`:

| Service | Ports | Purpose |
|---|---|---|
| **Grafana (LGTM)** | 3000 (UI), 4317 (OTLP) | OpenTelemetry traces and metrics |
| **Azurite** | 10000–10002 | Azure Storage emulator (unused by the codebase) |

### Build

```bash
dotnet build
```

### Run

```bash
cd src/MassTransitDemo.Console
dotnet run
```

This launches an interactive menu where you can select any use case by number.

### Run Non-interactively

```bash
cd src/MassTransitDemo.Console
dotnet run -- --demo <name-or-number>
```

Valid demo values: `1`–`8`, or `basic-messaging`, `handler-chain`, `error-handling`, `retry`, `outbox`, `consumer-saga`, `state-machine-saga`, `topic-fanout`.

### CLI Options

| Option | Short | Description |
|---|---|---|
| `--demo` | `-d` | Run a specific demo non-interactively |
| `--transport` | `-t` | Override transport (`RabbitMQ`, `AzureServiceBus`, `PostgreSQL`) |
| `--saga-order` | `-s` | Saga event arrival order (`order-first`, `inventory-first`, `concurrent`) |
| `--saga-persistence` | `-p` | Saga persistence strategy (`InMemory`, `MessageSession`, `EntityFramework`) |

## Configuration

### Transport Selection

Set the transport in `src/MassTransitDemo.Console/appsettings.json`:

```json
{
  "Transport": {
    "TransportType": "RabbitMQ"
  }
}
```

Supported transports:

- **RabbitMQ** — uses the RabbitMQ instance from Docker Compose
- **PostgreSQL** — uses the PostgreSQL instance from Docker Compose as both transport and outbox store
- **Azure Service Bus** — requires a connection string or the Azurite emulator

### Environment Variables

Connection strings can be overridden with environment variables:

- `RABBITMQ_CONNECTION_STRING`
- `POSTGRESQL_CONNECTION_STRING`
- `AZURE_SERVICE_BUS_CONNECTION_STRING`

### OpenTelemetry

Traces and metrics are exported via OTLP (gRPC) to the endpoint configured in `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

Start the Grafana LGTM stack to receive telemetry: `docker compose up -d grafana`.

## Project Structure

```
src/
├── MassTransitDemo.Console/                  # Host, DI, menu, CLI
├── MassTransitDemo.Core/                     # Shared message contracts & transport abstractions
├── MassTransitDemo.Transports/               # Transport configurators (RabbitMQ, PostgreSQL, ASB)
├── MassTransitDemo.Features.BasicMessaging/  # Use cases 1 & 2 — basic pub/sub, handler chain
├── MassTransitDemo.Features.ErrorHandling/   # Use cases 3 & 4 — DLQ and retry
├── MassTransitDemo.Features.Outbox/          # Use case 5 — transactional outbox
├── MassTransitDemo.Features.Sagas/           # Use cases 6 & 7 — consumer & state machine sagas
└── MassTransitDemo.Features.TopicFanout/     # Use case 8 — pub/sub fan-out

tests/
├── MassTransitDemo.Tests.BasicMessaging/     # Handler and endpoint-formatter tests
├── MassTransitDemo.Tests.ErrorHandling/      # Payment failure and retry tests
├── MassTransitDemo.Tests.Outbox/             # Order creation and outbox publish tests
├── MassTransitDemo.Tests.Sagas/              # Saga state progression tests
└── MassTransitDemo.Tests.TopicFanout/        # Fan-out handler tests
```

## Testing

All test projects use in-memory fakes — no Docker or external services required.

```bash
dotnet test
```

## License

MIT
