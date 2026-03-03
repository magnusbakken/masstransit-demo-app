using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Outbox.Data;
using MassTransitDemo.Features.Outbox.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MassTransitDemo.Tests.Outbox.Handlers;

public sealed class CreateOrderHandlerTests
{
    [Fact]
    public async Task Consume_CreateOrderCommand_AddsOrderToDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new OutboxDbContext(options);
        var loggerMock = new Mock<ILogger<CreateOrderHandler>>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();

        var handler = new CreateOrderHandler(dbContext, loggerMock.Object, publishEndpointMock.Object);

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

        var context = Mock.Of<ConsumeContext<CreateOrder>>(c => c.Message == message);

        // Act
        await handler.Consume(context);

        // Assert
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == message.OrderId);
        Assert.NotNull(order);
        Assert.Equal(message.OrderId, order.OrderId);
        Assert.Equal(message.CustomerId, order.CustomerId);
        Assert.Equal(message.TotalAmount, order.TotalAmount);

        publishEndpointMock.Verify(
            x => x.Publish(It.Is<OrderCreated>(m => m.OrderId == message.OrderId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
