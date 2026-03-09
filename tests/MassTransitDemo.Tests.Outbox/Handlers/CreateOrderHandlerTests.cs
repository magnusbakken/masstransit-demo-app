using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Outbox.Data;
using MassTransitDemo.Features.Outbox.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.Outbox.Handlers;

public sealed class CreateOrderHandlerTests
{
    [Test]
    public async Task Consume_CreateOrderCommand_AddsOrderToDatabaseAndPublishesViaContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new OutboxDbContext(options);
        var loggerFake = A.Fake<ILogger<CreateOrderHandler>>();

        // CreateOrderHandler now depends only on OutboxDbContext + ILogger.
        var handler = new CreateOrderHandler(dbContext, loggerFake);

        var message = new CreateOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 199.99m,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductName = "Widget A", Quantity = 2, UnitPrice = 50.00m }
            }
        };

        // ConsumeContext<T> extends IPublishEndpoint, so FakeItEasy can intercept
        // context.Publish() calls — exactly as the real outbox middleware would.
        var context = A.Fake<ConsumeContext<CreateOrder>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await handler.Consume(context);

        // Assert — business record persisted
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == message.OrderId);
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.OrderId).IsEqualTo(message.OrderId);
        await Assert.That(order.CustomerId).IsEqualTo(message.CustomerId);
        await Assert.That(order.TotalAmount).IsEqualTo(message.TotalAmount);

        // Assert — OrderCreated published via ConsumeContext (not IPublishEndpoint),
        // which the real outbox middleware intercepts and writes to OutboxMessage.
        A.CallTo(() => context.Publish(
                A<OrderCreated>.That.Matches(m => m.OrderId == message.OrderId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
