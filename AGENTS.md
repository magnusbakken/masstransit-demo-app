# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

MassTransit Demo — a .NET 10 console application showcasing MassTransit 8.x messaging patterns (pub/sub, error handling, transactional outbox, sagas). Single solution (`MassTransitDemo.slnx`) with 7 source projects and 4 test projects.

### Prerequisites

- **.NET 10 SDK** (`10.0.103`, `rollForward: patch` per `global.json`)
- **Docker + Docker Compose** for PostgreSQL and RabbitMQ

### Infrastructure services

Start before running the console app:

```bash
docker compose up -d postgres rabbitmq
```

- **PostgreSQL** (port 5432) — used for EF Core outbox and optionally as transport
- **RabbitMQ** (port 5672, management UI 15672) — default message transport

Credentials for both: `masstransit` / `masstransit`. Database: `masstransit_demo`.

### Build / Test / Run

Standard commands per `README.md`:

- **Build:** `dotnet build`
- **Test:** `dotnet test` — all tests use in-memory fakes, no Docker required
- **Run:** `dotnet run --project src/MassTransitDemo.Console` (must run from repo root, or `cd src/MassTransitDemo.Console && dotnet run`)

### Gotchas

- The console app reads `appsettings.json` relative to the working directory. If you run `dotnet run --project src/MassTransitDemo.Console` from the repo root, it fails with `FileNotFoundException` for `appsettings.json`. Run from the project directory instead: `cd src/MassTransitDemo.Console && dotnet run`.
- The app uses `Console.ReadKey()` for interactive menus, which throws `InvalidOperationException` when stdin is piped. For automated testing, pipe menu choices via `printf '1\n' | dotnet run` — the event will be published successfully before the `ReadKey` exception.
- `TreatWarningsAsErrors` is enabled in `Directory.Build.props` — all compiler warnings are errors.
- The `MassTransitDemo.Tests.Outbox` project currently reports 0 tests (TUnit source generation issue) — this is a pre-existing condition, not an environment problem.
- Azurite is defined in `docker-compose.yml` but unused by the codebase. No need to start it.
- Docker daemon in Cloud Agent VMs requires `fuse-overlayfs` storage driver and `iptables-legacy`. These are configured during initial setup.
