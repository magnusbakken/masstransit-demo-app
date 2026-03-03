using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.ErrorHandling.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MassTransitDemo.Tests.ErrorHandling.Handlers;

public sealed class ProcessPaymentHandlerTests
{
    [Fact]
    public async Task Consume_ProcessPaymentCommand_ThrowsException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ProcessPaymentHandler>>();
        var handler = new ProcessPaymentHandler(loggerMock.Object);

        var message = new ProcessPayment
        {
            PaymentId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            PaymentMethod = "Credit Card"
        };

        var context = Mock.Of<ConsumeContext<ProcessPayment>>(c => c.Message == message);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Consume(context));
    }
}
