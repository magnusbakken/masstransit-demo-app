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
    public async Task Consume_CreateOrderCommand_AddsOrderToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new OutboxDbContext(options);
        var loggerFake = A.Fake<ILogger<CreateOrderHandler>>();
        var publishEndpointFake = A.Fake<IPublishEndpoint>();

        var handler = new CreateOrderHandler(dbContext, loggerFake, publishEndpointFake);

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

        var context = A.Fake<ConsumeContext<CreateOrder>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await handler.Consume(context);

        // Assert
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == message.OrderId);
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.OrderId).IsEqualTo(message.OrderId);
        await Assert.That(order.CustomerId).IsEqualTo(message.CustomerId);
        await Assert.That(order.TotalAmount).IsEqualTo(message.TotalAmount);

        A.CallTo(() => publishEndpointFake.Publish(
                A<OrderCreated>.That.Matches(m => m.OrderId == message.OrderId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
