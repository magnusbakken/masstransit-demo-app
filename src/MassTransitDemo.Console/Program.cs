using System.CommandLine;
using MassTransit;
using MassTransit.Logging;
using MassTransit.Monitoring;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MassTransitDemo.Console;

public static partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        var demoOption = new Option<string?>("--demo", "-d")
        {
            Description =
                "Demo to run non-interactively. Accepts a number (1-8) or a name: " +
                "basic-messaging, handler-chain, error-handling, retry, outbox, " +
                "consumer-saga, state-machine-saga, topic-fanout.",
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

        var rootCommand = new RootCommand(
            "MassTransit Demo — showcase of MassTransit 8.x messaging patterns. " +
            "Run without arguments to launch the interactive menu. " +
            "Start MassTransitDemo.Worker in a separate terminal to process messages.");

        rootCommand.Add(demoOption);
        rootCommand.Add(transportOption);
        rootCommand.Add(sagaOrderOption);

        rootCommand.SetAction(async parseResult =>
        {
            var demo = parseResult.GetValue(demoOption);
            var transport = parseResult.GetValue(transportOption);
            var sagaOrder = parseResult.GetValue(sagaOrderOption) ?? "order-first";

            await RunApplicationAsync(demo, transport, sagaOrder);
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task RunApplicationAsync(
        string? demo, string? transport, string sagaOrder)
    {
        var host = CreateHostBuilder(transport).Build();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MassTransitDemo");
        var transportOptions = host.Services.GetRequiredService<TransportOptions>();
        logger.LogInformation("MassTransit Demo Console starting...");
        logger.LogInformation("Transport: {TransportType}", transportOptions.TransportType);

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
            host.Services.GetService<TracerProvider>()?.ForceFlush();
            host.Services.GetService<MeterProvider>()?.ForceFlush();
            host.Services.GetService<LoggerProvider>()?.ForceFlush();

            await host.StopAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string? transportOverride = null) =>
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
                    PostgreSQLConnectionString = transportSection["PostgreSQLConnectionString"]
                };

                services.AddSingleton(transportOptions);

                var transportConfigurator = TransportConfiguratorFactory.Create(transportOptions);

                services.AddMassTransit(x =>
                {
                    var username = Environment.UserName;
                    x.SetEndpointNameFormatter(
                        new PrefixedKebabCaseEndpointNameFormatter($"masstransitdemo.{username}"));

                    // No consumers registered — the Console is a publisher/sender only.
                    // All consumers run in MassTransitDemo.Worker.

                    transportConfigurator.Configure(x);
                });

                var otlpEndpoint = context.Configuration["OpenTelemetry:OtlpEndpoint"]
                                   ?? "http://localhost:4317";

                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService(
                            serviceName: "MassTransitDemo",
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
            });
}
