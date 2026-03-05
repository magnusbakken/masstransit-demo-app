using MassTransit;
using MassTransitDemo.Core.Messages;
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
            default:
                System.Console.Error.WriteLine(
                    $"Unknown demo '{demo}'. Valid values: 1-7 or basic-messaging, " +
                    "handler-chain, error-handling, retry, outbox, consumer-saga, state-machine-saga.");
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
            System.Console.WriteLine("3. Error Handling - ProcessPayment (fails, goes to DLQ)");
            System.Console.WriteLine("4. Retry Mechanism - ProcessOrder (fails first time, succeeds on retry)");
            System.Console.WriteLine("5. Transactional Outbox - CreateOrder (database + event atomically)");
            System.Console.WriteLine("6. Consumer Saga - Shipment Preparation (OrderConfirmed or InventoryReserved)");
            System.Console.WriteLine("7. State Machine Saga - Shipment Preparation (OrderConfirmed or InventoryReserved)");
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

        System.Console.WriteLine();
        System.Console.WriteLine("=== Error Handling Demo ===");
        System.Console.WriteLine("Publishing ProcessPayment command that will fail...");
        System.Console.WriteLine("This message will be moved to the dead-letter queue after failure.");
        System.Console.WriteLine();

        var processPayment = new ProcessPayment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            PaymentMethod = "Credit Card"
        };

        await bus.Publish(processPayment);

        logger.LogInformation("ProcessPayment command published - PaymentId: {PaymentId}",
            processPayment.PaymentId);

        System.Console.WriteLine("Command published! The handler will fail and the message will be moved to DLQ.");
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

        System.Console.WriteLine();
        System.Console.WriteLine("=== Retry Mechanism Demo ===");
        System.Console.WriteLine("Publishing ProcessOrder command that will fail first time, succeed on retry...");
        System.Console.WriteLine("Retry policy: Exponential backoff (5 attempts max)");
        System.Console.WriteLine();

        var processOrder = new ProcessOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 149.99m
        };

        await bus.Publish(processOrder);

        logger.LogInformation("ProcessOrder command published - OrderId: {OrderId}", processOrder.OrderId);

        System.Console.WriteLine("Command published! Watch for:");
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

        System.Console.WriteLine();
        System.Console.WriteLine("=== Transactional Outbox Demo ===");
        System.Console.WriteLine("Publishing CreateOrder command...");
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

        await bus.Publish(createOrder);

        logger.LogInformation("CreateOrder command published - OrderId: {OrderId}", createOrder.OrderId);

        System.Console.WriteLine("Command published! The handler will:");
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

        await PublishSagaEventsAsync(bus, orderId, customerId, choice);

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

        await PublishSagaEventsAsync(bus, orderId, customerId, choice);

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

    private static async Task PublishSagaEventsAsync(
        IBus bus, Guid orderId, Guid customerId, string choice)
    {
        var reservedItems = new List<ReservedItem>
        {
            new ReservedItem { ProductId = "PROD-001", Quantity = 2 }
        };

        switch (choice)
        {
            case "1":
                await bus.Publish(new OrderConfirmed
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    ConfirmedAt = DateTime.UtcNow
                });
                await Task.Delay(1000);
                await bus.Publish(new InventoryReserved
                {
                    OrderId = orderId,
                    Items = reservedItems,
                    ReservedAt = DateTime.UtcNow
                });
                break;
            case "2":
                await bus.Publish(new InventoryReserved
                {
                    OrderId = orderId,
                    Items = reservedItems,
                    ReservedAt = DateTime.UtcNow
                });
                await Task.Delay(1000);
                await bus.Publish(new OrderConfirmed
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    ConfirmedAt = DateTime.UtcNow
                });
                break;
            case "3":
                await Task.WhenAll(
                    bus.Publish(new OrderConfirmed
                    {
                        OrderId = orderId,
                        CustomerId = customerId,
                        ConfirmedAt = DateTime.UtcNow
                    }),
                    bus.Publish(new InventoryReserved
                    {
                        OrderId = orderId,
                        Items = reservedItems,
                        ReservedAt = DateTime.UtcNow
                    }));
                break;
            default:
                System.Console.WriteLine("Invalid choice. Using default (OrderConfirmed first).");
                await bus.Publish(new OrderConfirmed
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    ConfirmedAt = DateTime.UtcNow
                });
                await Task.Delay(1000);
                await bus.Publish(new InventoryReserved
                {
                    OrderId = orderId,
                    Items = reservedItems,
                    ReservedAt = DateTime.UtcNow
                });
                break;
        }
    }
}