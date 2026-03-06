using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using MassTransitDemo.Features.Outbox.Handlers;
using MassTransitDemo.Features.Sagas.ConsumerSaga;
using MassTransitDemo.Features.Sagas.StateMachineSaga;
using MassTransitDemo.Features.TopicFanout.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Console;

public static partial class Program
{
    private static async Task RunDemoNonInteractiveAsync(
        IServiceProvider services, ILogger logger, string demo, string sagaOrder)
    {
        var key = demo.Trim().ToLowerInvariant();
        switch (key)
        {
            case "1" or "basic-messaging":
                await HandleBasicMessagingAsync(services, logger, interactive: false);
                break;
            case "2" or "handler-chain":
                await HandleHandlerChainAsync(services, logger, interactive: false);
                break;
            case "3" or "error-handling":
                await HandleErrorHandlingAsync(services, logger, interactive: false);
                break;
            case "4" or "retry":
                await HandleRetryMechanismAsync(services, logger, interactive: false);
                break;
            case "5" or "outbox":
                await HandleTransactionalOutboxAsync(services, logger, interactive: false);
                break;
            case "6" or "consumer-saga":
                await HandleConsumerSagaAsync(services, logger, sagaOrder, interactive: false);
                break;
            case "7" or "state-machine-saga":
                await HandleStateMachineSagaAsync(services, logger, sagaOrder, interactive: false);
                break;
            case "8" or "topic-fanout":
                await HandleTopicFanoutAsync(services, logger, interactive: false);
                break;
            default:
                System.Console.Error.WriteLine(
                    $"Unknown demo '{demo}'. Valid values: 1-8 or basic-messaging, " +
                    "handler-chain, error-handling, retry, outbox, consumer-saga, " +
                    "state-machine-saga, topic-fanout.");
                break;
        }
    }

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
            System.Console.WriteLine("3. Error Handling - Send ProcessPayment command (fails, goes to DLQ)");
            System.Console.WriteLine("4. Retry Mechanism - Send ProcessOrder command (fails first time, succeeds on retry)");
            System.Console.WriteLine("5. Transactional Outbox - Send CreateOrder command (database + event atomically)");
            System.Console.WriteLine("6. Consumer Saga - Shipment Preparation (OrderConfirmed or InventoryReserved)");
            System.Console.WriteLine("7. State Machine Saga - Shipment Preparation (OrderConfirmed or InventoryReserved)");
            System.Console.WriteLine("8. Topic Fan-out - Publish OrderShipped event to two independent consumers");
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
                    await HandleErrorHandlingAsync(services, logger);
                    break;
                case "4":
                    await HandleRetryMechanismAsync(services, logger);
                    break;
                case "5":
                    await HandleTransactionalOutboxAsync(services, logger);
                    break;
                case "6":
                    await HandleConsumerSagaAsync(services, logger);
                    break;
                case "7":
                    await HandleStateMachineSagaAsync(services, logger);
                    break;
                case "8":
                    await HandleTopicFanoutAsync(services, logger);
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

    private static async Task HandleBasicMessagingAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();

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

        logger.LogInformation("CustomerCreated event published - CustomerId: {CustomerId}",
            customerCreated.CustomerId);

        System.Console.WriteLine("Event published! Check the logs above for handler output.");
        System.Console.WriteLine("Note: This will also trigger the handler chain (CustomerCreated → SendVerificationEmail → ...)");

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleHandlerChainAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();

        System.Console.WriteLine();
        System.Console.WriteLine("=== Handler Chain Demo ===");
        System.Console.WriteLine("Publishing CustomerCreated event to trigger the chain...");
        System.Console.WriteLine("Chain: CustomerCreated → SendVerificationEmail → EmailSent → WelcomeEmailSent");
        System.Console.WriteLine("Events are published (fan-out); commands are sent point-to-point.");
        System.Console.WriteLine();

        var customerCreated = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Jane Smith",
            Email = "jane.smith@example.com"
        };

        await bus.Publish(customerCreated);

        logger.LogInformation("CustomerCreated event published to trigger chain - CustomerId: {CustomerId}",
            customerCreated.CustomerId);

        System.Console.WriteLine("Chain initiated! Watch the console for each step in the chain.");
        System.Console.WriteLine("Waiting 2 seconds for chain to complete...");
        await Task.Delay(2000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleErrorHandlingAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();
        var formatter = services.GetRequiredService<IEndpointNameFormatter>();

        System.Console.WriteLine();
        System.Console.WriteLine("=== Error Handling Demo ===");
        System.Console.WriteLine("Sending ProcessPayment command that will fail...");
        System.Console.WriteLine("This message will be moved to the dead-letter queue after failure.");
        System.Console.WriteLine();

        var processPayment = new ProcessPayment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            PaymentMethod = "Credit Card"
        };

        // Send point-to-point — ProcessPayment is a command with one specific handler.
        var address = new Uri($"queue:{formatter.Consumer<ProcessPaymentHandler>()}");
        var endpoint = await bus.GetSendEndpoint(address);
        await endpoint.Send(processPayment);

        logger.LogInformation("ProcessPayment command sent - PaymentId: {PaymentId}",
            processPayment.PaymentId);

        System.Console.WriteLine("Command sent! The handler will fail and the message will be moved to DLQ.");
        System.Console.WriteLine("Check your transport's dead-letter queue to see the failed message.");

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleRetryMechanismAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();
        var formatter = services.GetRequiredService<IEndpointNameFormatter>();

        System.Console.WriteLine();
        System.Console.WriteLine("=== Retry Mechanism Demo ===");
        System.Console.WriteLine("Sending ProcessOrder command that will fail first time, succeed on retry...");
        System.Console.WriteLine("Retry policy: Exponential backoff (5 attempts max)");
        System.Console.WriteLine();

        var processOrder = new ProcessOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 149.99m
        };

        // Send point-to-point — ProcessOrder is a command with one specific handler.
        var address = new Uri($"queue:{formatter.Consumer<ProcessOrderHandler>()}");
        var endpoint = await bus.GetSendEndpoint(address);
        await endpoint.Send(processOrder);

        logger.LogInformation("ProcessOrder command sent - OrderId: {OrderId}", processOrder.OrderId);

        System.Console.WriteLine("Command sent! Watch for:");
        System.Console.WriteLine("1. First attempt will fail (intentional)");
        System.Console.WriteLine("2. Automatic retry with exponential backoff");
        System.Console.WriteLine("3. Second attempt will succeed");
        System.Console.WriteLine();
        System.Console.WriteLine("Waiting 5 seconds for retry to complete...");
        await Task.Delay(5000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleTransactionalOutboxAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();
        var formatter = services.GetRequiredService<IEndpointNameFormatter>();

        System.Console.WriteLine();
        System.Console.WriteLine("=== Transactional Outbox Demo ===");
        System.Console.WriteLine("Sending CreateOrder command...");
        System.Console.WriteLine("This will update the database and publish OrderCreated event atomically.");
        System.Console.WriteLine();

        var createOrder = new CreateOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 199.99m,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductName = "Widget A", Quantity = 2, UnitPrice = 50.00m },
                new OrderItem { ProductName = "Widget B", Quantity = 1, UnitPrice = 99.99m }
            }
        };

        // Send point-to-point — CreateOrder is a command with one specific handler.
        var address = new Uri($"queue:{formatter.Consumer<CreateOrderHandler>()}");
        var endpoint = await bus.GetSendEndpoint(address);
        await endpoint.Send(createOrder);

        logger.LogInformation("CreateOrder command sent - OrderId: {OrderId}", createOrder.OrderId);

        System.Console.WriteLine("Command sent! The handler will:");
        System.Console.WriteLine("1. Create order in database");
        System.Console.WriteLine("2. Publish OrderCreated event (stored in outbox)");
        System.Console.WriteLine("3. Commit transaction atomically");
        System.Console.WriteLine("4. Outbox delivery service will deliver event to broker");
        System.Console.WriteLine();
        System.Console.WriteLine("Waiting 3 seconds for outbox delivery...");
        await Task.Delay(3000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleConsumerSagaAsync(
        IServiceProvider services, ILogger logger,
        string sagaOrder = "order-first", bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();
        var formatter = services.GetRequiredService<IEndpointNameFormatter>();
        var sagaEndpoint = new Uri($"queue:{formatter.Saga<ShipmentPreparationSaga>()}");

        System.Console.WriteLine();
        System.Console.WriteLine("=== Consumer Saga Demo ===");
        System.Console.WriteLine("This saga can be initiated by either OrderConfirmed or InventoryReserved.");
        System.Console.WriteLine("It completes when both messages have been received.");
        System.Console.WriteLine();

        string choice;
        if (interactive)
        {
            System.Console.WriteLine("Choose initiation order:");
            System.Console.WriteLine("1. OrderConfirmed first, then InventoryReserved");
            System.Console.WriteLine("2. InventoryReserved first, then OrderConfirmed");
            System.Console.WriteLine("3. Both at the same time");
            System.Console.Write("Enter your choice (1-3): ");
            choice = System.Console.ReadLine() ?? "1";
        }
        else
        {
            choice = sagaOrder.ToLowerInvariant() switch
            {
                "1" or "order-first" => "1",
                "2" or "inventory-first" => "2",
                "3" or "concurrent" => "3",
                _ => "1"
            };
            System.Console.WriteLine($"Saga order: {sagaOrder} (choice {choice})");
        }

        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await SendSagaEventsAsync(bus, sagaEndpoint, orderId, customerId, choice);

        logger.LogInformation("Consumer saga demo initiated - OrderId: {OrderId}", orderId);

        System.Console.WriteLine();
        System.Console.WriteLine("Events published! Watch the console for saga progression.");
        System.Console.WriteLine("Waiting 3 seconds for saga to complete...");
        await Task.Delay(3000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleStateMachineSagaAsync(
        IServiceProvider services, ILogger logger,
        string sagaOrder = "order-first", bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();
        var formatter = services.GetRequiredService<IEndpointNameFormatter>();
        var sagaEndpoint = new Uri($"queue:{formatter.Saga<ShipmentPreparationState>()}");

        System.Console.WriteLine();
        System.Console.WriteLine("=== State Machine Saga Demo ===");
        System.Console.WriteLine("This saga can be initiated by either OrderConfirmed or InventoryReserved.");
        System.Console.WriteLine("It completes when both messages have been received.");
        System.Console.WriteLine();

        string choice;
        if (interactive)
        {
            System.Console.WriteLine("Choose initiation order:");
            System.Console.WriteLine("1. OrderConfirmed first, then InventoryReserved");
            System.Console.WriteLine("2. InventoryReserved first, then OrderConfirmed");
            System.Console.WriteLine("3. Both at the same time");
            System.Console.Write("Enter your choice (1-3): ");
            choice = System.Console.ReadLine() ?? "1";
        }
        else
        {
            choice = sagaOrder.ToLowerInvariant() switch
            {
                "1" or "order-first" => "1",
                "2" or "inventory-first" => "2",
                "3" or "concurrent" => "3",
                _ => "1"
            };
            System.Console.WriteLine($"Saga order: {sagaOrder} (choice {choice})");
        }

        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await SendSagaEventsAsync(bus, sagaEndpoint, orderId, customerId, choice);

        logger.LogInformation("State machine saga demo initiated - OrderId: {OrderId}", orderId);

        System.Console.WriteLine();
        System.Console.WriteLine("Events published! Watch the console for saga state transitions.");
        System.Console.WriteLine("Waiting 3 seconds for saga to complete...");
        await Task.Delay(3000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task HandleTopicFanoutAsync(
        IServiceProvider services, ILogger logger, bool interactive = true)
    {
        var bus = services.GetRequiredService<IBus>();

        System.Console.WriteLine();
        System.Console.WriteLine("=== Topic Fan-out Demo ===");
        System.Console.WriteLine("Publishing OrderShipped event to demonstrate pub/sub fan-out.");
        System.Console.WriteLine("Two independent consumers will each receive their own copy:");
        System.Console.WriteLine("  • ShippingNotificationHandler — notifies the customer");
        System.Console.WriteLine("  • WarehouseUpdateHandler      — updates warehouse records");
        System.Console.WriteLine();
        System.Console.WriteLine("Transport topology:");
        System.Console.WriteLine("  RabbitMQ      — fanout exchange binds to two separate queues");
        System.Console.WriteLine("  PostgreSQL    — topic with two independent subscriptions");
        System.Console.WriteLine("  Azure Svc Bus — topic with two independent subscriptions");
        System.Console.WriteLine();

        var orderShipped = new OrderShipped
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ShippedAt = DateTimeOffset.UtcNow,
            TrackingNumber = $"TRACK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
        };

        // Publish (not Send) — OrderShipped is an event. The transport routes it to
        // every consumer subscribed to this message type via its topic/exchange.
        await bus.Publish(orderShipped);

        logger.LogInformation(
            "OrderShipped event published - OrderId: {OrderId}, Tracking: {TrackingNumber}",
            orderShipped.OrderId,
            orderShipped.TrackingNumber);

        System.Console.WriteLine("Event published! Waiting 2 seconds for both handlers to complete...");
        await Task.Delay(2000);

        if (interactive)
        {
            System.Console.WriteLine("Press any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static async Task SendSagaEventsAsync(
        ISendEndpointProvider sendProvider, Uri sagaEndpoint,
        Guid orderId, Guid customerId, string choice)
    {
        var endpoint = await sendProvider.GetSendEndpoint(sagaEndpoint);

        var orderConfirmed = new OrderConfirmed
        {
            OrderId = orderId,
            CustomerId = customerId,
            ConfirmedAt = DateTime.UtcNow
        };

        var inventoryReserved = new InventoryReserved
        {
            OrderId = orderId,
            Items = [new ReservedItem { ProductId = "PROD-001", Quantity = 2 }],
            ReservedAt = DateTime.UtcNow
        };

        switch (choice)
        {
            case "1":
                await endpoint.Send(orderConfirmed);
                await Task.Delay(1000);
                await endpoint.Send(inventoryReserved);
                break;
            case "2":
                await endpoint.Send(inventoryReserved);
                await Task.Delay(1000);
                await endpoint.Send(orderConfirmed);
                break;
            case "3":
                await Task.WhenAll(
                    endpoint.Send(orderConfirmed),
                    endpoint.Send(inventoryReserved));
                break;
            default:
                System.Console.WriteLine("Invalid choice. Using default (OrderConfirmed first).");
                await endpoint.Send(orderConfirmed);
                await Task.Delay(1000);
                await endpoint.Send(inventoryReserved);
                break;
        }
    }
}
