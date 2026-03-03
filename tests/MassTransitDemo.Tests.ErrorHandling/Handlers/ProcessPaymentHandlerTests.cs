using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.ErrorHandling.Handlers;

public sealed class ProcessPaymentHandlerTests
{
    [Test]
    public async Task Consume_ProcessPaymentCommand_ThrowsException()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<ProcessPaymentHandler>>();
        var handler = new ProcessPaymentHandler(loggerFake);

        var message = new ProcessPayment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            PaymentMethod = "Credit Card"
        };

        var context = A.Fake<ConsumeContext<ProcessPayment>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Consume(context));
    }
}
