using System.CommandLine;
using MassTransit;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Features.BasicMessaging.Handlers;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using MassTransitDemo.Features.Outbox.Data;
using MassTransitDemo.Features.Outbox.Handlers;
using MassTransitDemo.Features.Sagas;
using MassTransitDemo.Features.Sagas.ConsumerSaga;
using MassTransitDemo.Features.Sagas.Handlers;
using MassTransitDemo.Features.Sagas.StateMachineSaga;
using MassTransitDemo.Transports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Console;

public static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        var demoOption = new Option<string?>("--demo", "-d")
        {
            Description =
                "Demo to run non-interactively. Accepts a number (1-7) or a name: " +
                "basic-messaging, handler-chain, error-handling, retry, outbox, " +
                "consumer-saga, state-machine-saga.",
            HelpName = "name"
        };

        var transportOption = new Option<string?>("--transport", "-t")
        {
            Description = "Transport type override: RabbitMQ, AzureServiceBus, PostgreSQL. " +
                          "Overrides the value in appsettings.json.",
            HelpName = "type"
        };

        var sagaOrderOption = new Option<string?>("--saga-order", "-s")
        {
            Description =
                "Event initiation order for saga demos (consumer-saga, state-machine-saga). " +
                "Values: order-first (default), inventory-first, concurrent.",
            HelpName = "order"
        };

        var messageSessionOption = new Option<bool?>("--message-session", "-m")
        {
            Description =
                "Use Azure Service Bus message sessions for saga state storage " +
                "(MessageSessionSagaRepository). Overrides the value in appsettings.json."
        };

        var rootCommand = new RootCommand(
            "MassTransit Demo — showcase of MassTransit 8.x messaging patterns. " +
            "Run without arguments to launch the interactive menu.");

        rootCommand.Add(demoOption);
        rootCommand.Add(transportOption);
        rootCommand.Add(sagaOrderOption);
        rootCommand.Add(messageSessionOption);

        rootCommand.SetAction(async parseResult =>
        {
            var demo = parseResult.GetValue(demoOption);
            var transport = parseResult.GetValue(transportOption);
            var sagaOrder = parseResult.GetValue(sagaOrderOption) ?? "order-first";
            var messageSession = parseResult.GetValue(messageSessionOption);

            await RunApplicationAsync(demo, transport, sagaOrder, messageSession);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task RunApplicationAsync(
        string? demo, string? transport, string sagaOrder, bool? messageSession)
    {
        var host = CreateHostBuilder(transport, messageSession).Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MassTransitDemo");
        logger.LogInformation("MassTransit Demo Application starting...");
        logger.LogInformation("Transport: {TransportType}",
            host.Services.GetRequiredService<IConfiguration>()
                .GetSection("Transport")["TransportType"]);

        // Ensure the EF Core outbox schema exists before starting the bus.
        // The outbox middleware is applied to all consumer endpoints, so the DB tables
        // must exist before the receive endpoints start polling them.
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Start the Generic Host so that MassTransit hosted services (bus + receive endpoints)
        // are fully running before any messages are published or the menu is shown.
        await host.StartAsync();

        try
        {
            if (demo is not null)
            {
                await RunDemoNonInteractiveAsync(host.Services, logger, demo, sagaOrder);
            }
            else
            {
                await DisplayMenuAsync(host.Services, logger);
            }
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(
        string? transportOverride = null, bool? messageSessionOverride = null) =>
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
                    UseMessageSessionSagaRepository = messageSessionOverride
                        ?? transportSection.GetValue<bool>("UseMessageSessionSagaRepository", false)
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

                var transportConfigurator = TransportConfiguratorFactory.Create(transportOptions);

                if (transportOptions.UseMessageSessionSagaRepository)
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

                    if (transportOptions.UseMessageSessionSagaRepository)
                    {
                        x.AddSaga<ShipmentPreparationSaga>(typeof(ShipmentPreparationSagaDefinition))
                            .MessageSessionRepository();

                        x.AddSagaStateMachine<ShipmentPreparationStateMachine, ShipmentPreparationState>(
                                typeof(ShipmentPreparationStateMachineDefinition))
                            .MessageSessionRepository();
                    }
                    else
                    {
                        x.AddSaga<ShipmentPreparationSaga>(typeof(ShipmentPreparationSagaDefinition))
                            .InMemoryRepository();

                        x.AddSagaStateMachine<ShipmentPreparationStateMachine, ShipmentPreparationState>(
                                typeof(ShipmentPreparationStateMachineDefinition))
                            .InMemoryRepository();
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
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            });
}
