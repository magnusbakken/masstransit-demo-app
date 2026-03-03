# MassTransit Demo Application

A comprehensive demo application showcasing MassTransit 8.x features with multiple transport options, messaging patterns, error handling, transactional outbox, and sagas.

## Features

- **Multiple Transport Support**: Azure Service Bus, RabbitMQ, and PostgreSQL
- **Basic Messaging**: Simple handlers and handler chains
- **Error Handling**: Dead-letter queues and retry mechanisms
- **Transactional Outbox**: Bus Outbox and Entity Framework Outbox patterns
- **Sagas**: Consumer-based and state machine sagas

## Prerequisites

- .NET 10 SDK (or .NET 8+ with rollForward configured)
- Docker and Docker Compose
- (Optional) Azure Service Bus connection string for production Azure Service Bus usage

## Setup

### 1. Start Infrastructure Services

Start the required infrastructure services using Docker Compose:

```bash
docker-compose up -d
```

This will start:
- **PostgreSQL** on port 5432 (for transport and outbox)
- **RabbitMQ** on ports 5672 (AMQP) and 15672 (Management UI)
- **Azurite** (Azure Storage Emulator) on ports 10000-10002

### 2. Configure Transport

Edit `src/MassTransitDemo.Console/appsettings.json` to select your preferred transport:
- `AzureServiceBus` - Use Azure Service Bus (emulator or real instance)
- `RabbitMQ` - Use RabbitMQ
- `PostgreSQL` - Use PostgreSQL as transport

### 3. Build and Run

```bash
dotnet build
dotnet run --project src/MassTransitDemo.Console
```

## Project Structure

```
src/
├── MassTransitDemo.Console/      # Main console application
├── MassTransitDemo.Core/         # Shared contracts and abstractions
└── MassTransitDemo.Transports/   # Transport configurations

src/MassTransitDemo.Features.*/   # Feature modules (added in later PRs)
tests/MassTransitDemo.Tests.*/    # Test projects (added in later PRs)
```

## Configuration

### Transport Selection

The application supports three transport options configured via `appsettings.json`:

- **Azure Service Bus**: Can use Azurite emulator (default) or a real Azure Service Bus instance via connection string
- **RabbitMQ**: Uses the RabbitMQ instance from Docker Compose
- **PostgreSQL**: Uses the PostgreSQL instance from Docker Compose

### Environment Variables

Connection strings can be overridden using environment variables:
- `AZURE_SERVICE_BUS_CONNECTION_STRING` - Azure Service Bus connection string
- `RABBITMQ_CONNECTION_STRING` - RabbitMQ connection string
- `POSTGRESQL_CONNECTION_STRING` - PostgreSQL connection string

## Testing

Run all tests:

```bash
dotnet test
```

## Docker Services

### PostgreSQL
- **Host**: localhost
- **Port**: 5432
- **Username**: masstransit
- **Password**: masstransit
- **Database**: masstransit_demo

### RabbitMQ
- **AMQP Port**: 5672
- **Management UI**: http://localhost:15672
- **Username**: masstransit
- **Password**: masstransit

### Azurite
- **Blob Service**: localhost:10000
- **Queue Service**: localhost:10001
- **Table Service**: localhost:10002

## Development

This project is organized into multiple pull requests:

1. **PR 1**: Project setup and infrastructure
2. **PR 2**: Transport configuration and basic console
3. **PR 3**: Basic messaging features
4. **PR 4**: Error handling and dead-letter queues
5. **PR 5**: Transactional outbox pattern
6. **PR 6**: Sagas

## License

MIT
