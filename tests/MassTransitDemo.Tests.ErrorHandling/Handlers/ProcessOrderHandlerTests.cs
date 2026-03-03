using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.ErrorHandling.Handlers;

public sealed class ProcessOrderHandlerTests
{
    [Test]
    public async Task Consume_ProcessOrderCommand_FirstAttemptThrowsException()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<ProcessOrderHandler>>();
        var handler = new ProcessOrderHandler(loggerFake);

        var message = new ProcessOrder
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalAmount = 149.99m
        };

        var context = A.Fake<ConsumeContext<ProcessOrder>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act & Assert - First attempt should fail
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Consume(context));
    }

    [Test]
    public async Task Consume_ProcessOrderCommand_SecondAttemptSucceeds()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<ProcessOrderHandler>>();
        var handler = new ProcessOrderHandler(loggerFake);

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

        var context1 = A.Fake<ConsumeContext<ProcessOrder>>();
        A.CallTo(() => context1.Message).Returns(message1);
        var context2 = A.Fake<ConsumeContext<ProcessOrder>>();
        A.CallTo(() => context2.Message).Returns(message2);

        // Act - First attempt fails
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Consume(context1));

        // Second attempt succeeds
        await handler.Consume(context2);
    }
}
