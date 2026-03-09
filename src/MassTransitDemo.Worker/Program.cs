using System.CommandLine;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Features.BasicMessaging.Handlers;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using MassTransitDemo.Features.Outbox.Data;
using MassTransitDemo.Features.Outbox.Handlers;
using MassTransitDemo.Features.Sagas;
using MassTransitDemo.Features.Sagas.ConsumerSaga;
using MassTransitDemo.Features.Sagas.Data;
using MassTransitDemo.Features.Sagas.Handlers;
using MassTransitDemo.Features.Sagas.StateMachineSaga;
using MassTransitDemo.Features.TopicFanout.Handlers;
using MassTransitDemo.Transports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MassTransitDemo.Worker;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var transportOption = new Option<string?>("--transport", "-t")
        {
            Description = "Transport type override: RabbitMQ, AzureServiceBus, PostgreSQL. " +
                          "Overrides the value in appsettings.json.",
            HelpName = "type"
        };

        var sagaPersistenceOption = new Option<string?>("--saga-persistence", "-p")
        {
            Description =
                "Saga persistence strategy: InMemory, MessageSession, EntityFramework. " +
                "Overrides the value in appsettings.json.",
            HelpName = "type"
        };

        var rootCommand = new RootCommand(
            "MassTransit Demo Worker — hosts all message consumers and sagas. " +
            "Start this before running the Console to process messages.");

        rootCommand.Add(transportOption);
        rootCommand.Add(sagaPersistenceOption);

        rootCommand.SetAction(async parseResult =>
        {
            var transport = parseResult.GetValue(transportOption);
            var sagaPersistence = parseResult.GetValue(sagaPersistenceOption);

            await RunWorkerAsync(transport, sagaPersistence);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task RunWorkerAsync(string? transport, string? sagaPersistence)
    {
        var host = CreateHostBuilder(transport, sagaPersistence).Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MassTransitDemo.Worker");
        var transportOptions = host.Services.GetRequiredService<TransportOptions>();

        logger.LogInformation("MassTransit Demo Worker starting...");
        logger.LogInformation("Transport: {TransportType}", transportOptions.TransportType);
        logger.LogInformation("Saga persistence: {SagaPersistenceType}", transportOptions.SagaPersistenceType);

        using (var scope = host.Services.CreateScope())
        {
            var outboxDb = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            await outboxDb.Database.EnsureCreatedAsync();

            if (transportOptions.SagaPersistenceType == SagaPersistenceType.EntityFramework)
            {
                var sagaDb = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
                var script = sagaDb.Database.GenerateCreateScript()
                    .Replace("CREATE TABLE", "CREATE TABLE IF NOT EXISTS");
                await sagaDb.Database.ExecuteSqlRawAsync(script);
            }
        }

        await host.RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(
        string? transportOverride = null, string? sagaPersistenceOverride = null) =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var transportSection = context.Configuration.GetSection("Transport");
                var transportOptions = new TransportOptions
                {
                    TransportType = transportOverride is not null
                        ? Enum.Parse<TransportType>(transportOverride, ignoreCase: true)
                        : Enum.Parse<TransportType>(
                            transportSection["TransportType"] ?? "RabbitMQ"),
                    UseAzureServiceBusEmulator =
                        transportSection.GetValue<bool>("UseAzureServiceBusEmulator", true),
                    UseAzureServiceBusNativeDlq =
                        transportSection.GetValue<bool>("UseAzureServiceBusNativeDlq", true),
                    AzureServiceBusConnectionString =
                        transportSection["AzureServiceBusConnectionString"],
                    RabbitMQConnectionString = transportSection["RabbitMQConnectionString"],
                    PostgreSQLConnectionString = transportSection["PostgreSQLConnectionString"],
                    SagaPersistenceType = sagaPersistenceOverride is not null
                        ? Enum.Parse<SagaPersistenceType>(sagaPersistenceOverride, ignoreCase: true)
                        : Enum.TryParse<SagaPersistenceType>(
                              transportSection["SagaPersistenceType"], ignoreCase: true, out var parsed)
                            ? parsed
                            : SagaPersistenceType.InMemory
                };

                services.AddSingleton(transportOptions);

                var postgresConnectionString =
                    string.IsNullOrEmpty(transportOptions.PostgreSQLConnectionString)
                        ? "Host=localhost;Port=5432;Database=masstransit_demo;Username=masstransit;Password=masstransit"
                        : transportOptions.PostgreSQLConnectionString;

                services.AddDbContext<OutboxDbContext>(options =>
                {
                    options.UseNpgsql(postgresConnectionString);
                });

                if (transportOptions.SagaPersistenceType == SagaPersistenceType.EntityFramework)
                {
                    services.AddDbContext<SagaDbContext>(options =>
                    {
                        options.UseNpgsql(postgresConnectionString);
                    });
                }

                var transportConfigurator = TransportConfiguratorFactory.Create(transportOptions);

                if (transportOptions.SagaPersistenceType == SagaPersistenceType.MessageSession)
                {
                    services.AddSingleton(new SessionEndpointConfigurator(cfg =>
                    {
                        if (cfg is IServiceBusReceiveEndpointConfigurator sb)
                            sb.RequiresSession = true;
                    }));
                }
                else
                {
                    services.AddSingleton(SessionEndpointConfigurator.NoOp);
                }

                services.AddMassTransit(x =>
                {
                    var username = Environment.UserName;
                    x.SetEndpointNameFormatter(
                        new PrefixedKebabCaseEndpointNameFormatter($"masstransitdemo.{username}"));

                    x.AddEntityFrameworkOutbox<OutboxDbContext>(o =>
                    {
                        o.UsePostgres();
                        o.UseBusOutbox();
                    });

                    x.AddConsumer<CustomerCreatedHandler>();
                    x.AddConsumer<SendVerificationEmailHandler>();
                    x.AddConsumer<EmailSentHandler>();
                    x.AddConsumer<WelcomeEmailSentHandler>();

                    x.AddConsumer<ProcessPaymentHandler>(cfg =>
                    {
                        cfg.UseMessageRetry(r => r.None());
                    });

                    x.AddConsumer<ProcessOrderHandler>(cfg =>
                    {
                        cfg.UseMessageRetry(r =>
                        {
                            r.Exponential(
                                5,
                                TimeSpan.FromSeconds(1),
                                TimeSpan.FromSeconds(10),
                                TimeSpan.FromSeconds(2));
                        });
                    });

                    x.AddConsumer<CreateOrderHandler>();
                    x.AddConsumer<OrderCreatedHandler>();

                    x.AddConsumer<ShipmentPreparedHandler>();

                    x.AddConsumer<ShippingNotificationHandler>();
                    x.AddConsumer<WarehouseUpdateHandler>();

                    switch (transportOptions.SagaPersistenceType)
                    {
                        case SagaPersistenceType.MessageSession:
                            x.AddSaga<ShipmentPreparationSaga>(typeof(ShipmentPreparationSagaDefinition))
                                .MessageSessionRepository();
                            x.AddSagaStateMachine<ShipmentPreparationStateMachine, ShipmentPreparationState>(
                                    typeof(ShipmentPreparationStateMachineDefinition))
                                .MessageSessionRepository();
                            break;

                        case SagaPersistenceType.EntityFramework:
                            x.AddSaga<ShipmentPreparationSaga>(typeof(ShipmentPreparationSagaDefinition))
                                .EntityFrameworkRepository(r =>
                                {
                                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                                    r.ExistingDbContext<SagaDbContext>();
                                    r.UsePostgres();
                                });
                            x.AddSagaStateMachine<ShipmentPreparationStateMachine, ShipmentPreparationState>(
                                    typeof(ShipmentPreparationStateMachineDefinition))
                                .EntityFrameworkRepository(r =>
                                {
                                    r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                                    r.ExistingDbContext<SagaDbContext>();
                                    r.UsePostgres();
                                });
                            break;

                        default:
                            x.AddSaga<ShipmentPreparationSaga>(typeof(ShipmentPreparationSagaDefinition))
                                .InMemoryRepository();
                            x.AddSagaStateMachine<ShipmentPreparationStateMachine, ShipmentPreparationState>(
                                    typeof(ShipmentPreparationStateMachineDefinition))
                                .InMemoryRepository();
                            break;
                    }

                    x.AddConfigureEndpointsCallback((context, _, cfg) =>
                    {
                        cfg.UseEntityFrameworkOutbox<OutboxDbContext>(context);
                    });

                    if (transportOptions.TransportType == TransportType.AzureServiceBus &&
                        transportOptions.UseAzureServiceBusNativeDlq)
                    {
                        x.AddConfigureEndpointsCallback((_, _, cfg) =>
                        {
                            if (cfg is IServiceBusReceiveEndpointConfigurator sb)
                            {
                                sb.ConfigureDeadLetterQueueDeadLetterTransport();
                                sb.ConfigureDeadLetterQueueErrorTransport();
                            }
                        });
                    }

                    transportConfigurator.Configure(x);
                });

                var otlpEndpoint = context.Configuration["OpenTelemetry:OtlpEndpoint"]
                                   ?? "http://localhost:4317";

                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(
                            serviceName: "MassTransitDemo.Worker",
                            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString()
                                            ?? "1.0.0"))
                    .WithTracing(tracing => tracing
                        .AddSource(DiagnosticHeaders.DefaultListenerName)
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(otlpEndpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        }))
                    .WithMetrics(metrics => metrics
                        .AddMeter(InstrumentationOptions.MeterName)
                        .AddRuntimeInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(otlpEndpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        }))
                    .WithLogging(logging => logging
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri(otlpEndpoint);
                            o.Protocol = OtlpExportProtocol.Grpc;
                        }));
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });
}
