using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MassTransitDemo.Tests.ErrorHandling.Handlers;

public sealed class ProcessOrderHandlerTests
{
    [Fact]
    public async Task Consume_ProcessOrderCommand_FirstAttemptThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ProcessOrderHandler>>();
        var handler = new ProcessOrderHandler(loggerMock.Object);

        var message = new ProcessOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 149.99m
        };

        var context = Mock.Of<ConsumeContext<ProcessOrder>>(c => c.Message == message);

        // Act & Assert - First attempt should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Consume(context));
    }

    [Fact]
    public async Task Consume_ProcessOrderCommand_SecondAttemptSucceeds()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ProcessOrderHandler>>();
        var handler = new ProcessOrderHandler(loggerMock.Object);

        var orderId = Guid.NewGuid();
        var message1 = new ProcessOrder
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            TotalAmount = 149.99m
        };

        var message2 = new ProcessOrder
        {
            OrderId = orderId,
            CustomerId = message1.CustomerId,
            TotalAmount = message1.TotalAmount
        };

        var context1 = Mock.Of<ConsumeContext<ProcessOrder>>(c => c.Message == message1);
        var context2 = Mock.Of<ConsumeContext<ProcessOrder>>(c => c.Message == message2);

        // Act - First attempt fails
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Consume(context1));

        // Second attempt succeeds
        await handler.Consume(context2);

        // Assert - No exception means success
        Assert.True(true);
    }
}
