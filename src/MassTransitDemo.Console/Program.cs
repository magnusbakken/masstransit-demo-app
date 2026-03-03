using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Features.BasicMessaging.Handlers;
using MassTransitDemo.Transports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Console;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("MassTransitDemo");
        logger.LogInformation("MassTransit Demo Application starting...");
        logger.LogInformation("Transport: {TransportType}", 
            host.Services.GetRequiredService<IConfiguration>()
                .GetSection("Transport")["TransportType"]);

        // Start the bus
        var bus = host.Services.GetRequiredService<IBus>();

        try
        {
            // Display menu
            await DisplayMenuAsync(host.Services, logger);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configure transport options
                var transportSection = context.Configuration.GetSection("Transport");
                var transportOptions = new TransportOptions
                {
                    TransportType = Enum.Parse<TransportType>(
                        transportSection["TransportType"] ?? "RabbitMQ"),
                    UseAzureServiceBusEmulator = transportSection.GetValue<bool>("UseAzureServiceBusEmulator", true),
                    AzureServiceBusConnectionString = transportSection["AzureServiceBusConnectionString"],
                    RabbitMQConnectionString = transportSection["RabbitMQConnectionString"],
                    PostgreSQLConnectionString = transportSection["PostgreSQLConnectionString"]
                };

                services.AddSingleton(transportOptions);

                // Configure PostgreSQL SQL transport options if using PostgreSQL
                // Note: This requires the MassTransit.SqlTransport.PostgreSQL package
                // and uses SqlTransportOptions which should be available via that package
                if (transportOptions.TransportType == TransportType.PostgreSQL)
                {
                    // PostgreSQL transport configuration will be handled by the transport configurator
                    // SqlTransportOptions can be configured here if needed via DI
                }

                // Create and configure transport
                var transportConfigurator = TransportConfiguratorFactory.Create(transportOptions);
                
                services.AddMassTransit(x =>
                {
                    // Register basic messaging handlers
                    x.AddConsumer<CustomerCreatedHandler>();
                    x.AddConsumer<SendVerificationEmailHandler>();
                    x.AddConsumer<EmailSentHandler>();
                    x.AddConsumer<WelcomeEmailSentHandler>();

                    transportConfigurator.Configure(x);
                });
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

    private static async Task DisplayMenuAsync(IServiceProvider services, ILogger logger)
    {
        while (true)
        {
            System.Console.Clear();
            System.Console.WriteLine("=== MassTransit Demo Application ===");
            System.Console.WriteLine();
            System.Console.WriteLine("Select a feature to test:");
            System.Console.WriteLine("1. Basic Messaging - Publish CustomerCreated event");
            System.Console.WriteLine("2. Handler Chain - Trigger CustomerCreated → SendVerificationEmail → EmailSent → WelcomeEmailSent");
            System.Console.WriteLine("3. Error Handling (coming in PR 4)");
            System.Console.WriteLine("4. Retry Mechanism (coming in PR 4)");
            System.Console.WriteLine("5. Transactional Outbox (coming in PR 5)");
            System.Console.WriteLine("6. Sagas (coming in PR 6)");
            System.Console.WriteLine("0. Exit");
            System.Console.WriteLine();
            System.Console.Write("Enter your choice: ");

            var choice = System.Console.ReadLine();

            switch (choice)
            {
                case "0":
                    logger.LogInformation("Exiting application...");
                    return;
                case "1":
                    await HandleBasicMessagingAsync(services, logger);
                    break;
                case "2":
                    await HandleHandlerChainAsync(services, logger);
                    break;
                case "3":
                case "4":
                case "5":
                case "6":
                    System.Console.WriteLine();
                    System.Console.WriteLine("This feature will be available in a future PR.");
                    System.Console.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    break;
                default:
                    System.Console.WriteLine();
                    System.Console.WriteLine("Invalid choice. Please try again.");
                    System.Console.WriteLine("Press any key to continue...");
                    System.Console.ReadKey();
                    break;
            }
        }
    }

    private static async Task HandleBasicMessagingAsync(IServiceProvider services, ILogger logger)
    {
        var bus = services.GetRequiredService<IPublishEndpoint>();
        
        System.Console.WriteLine();
        System.Console.WriteLine("=== Basic Messaging Demo ===");
        System.Console.WriteLine("Publishing CustomerCreated event...");
        System.Console.WriteLine();

        var customerCreated = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "John Doe",
            Email = "john.doe@example.com"
        };

        await bus.Publish(customerCreated);

        logger.LogInformation("CustomerCreated event published - CustomerId: {CustomerId}", customerCreated.CustomerId);
        
        System.Console.WriteLine("Event published! Check the logs above for handler output.");
        System.Console.WriteLine("Note: This will also trigger the handler chain (CustomerCreated → SendVerificationEmail → ...)");
        System.Console.WriteLine("Press any key to continue...");
        System.Console.ReadKey();
    }

    private static async Task HandleHandlerChainAsync(IServiceProvider services, ILogger logger)
    {
        var bus = services.GetRequiredService<IPublishEndpoint>();
        
        System.Console.WriteLine();
        System.Console.WriteLine("=== Handler Chain Demo ===");
        System.Console.WriteLine("Publishing CustomerCreated event to trigger the chain...");
        System.Console.WriteLine("Chain: CustomerCreated → SendVerificationEmail → EmailSent → WelcomeEmailSent");
        System.Console.WriteLine();

        var customerCreated = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Jane Smith",
            Email = "jane.smith@example.com"
        };

        await bus.Publish(customerCreated);

        logger.LogInformation("CustomerCreated event published to trigger chain - CustomerId: {CustomerId}", customerCreated.CustomerId);
        
        System.Console.WriteLine("Chain initiated! Watch the console for each step in the chain.");
        System.Console.WriteLine("Waiting 2 seconds for chain to complete...");
        await Task.Delay(2000);
        System.Console.WriteLine("Press any key to continue...");
        System.Console.ReadKey();
    }
}
