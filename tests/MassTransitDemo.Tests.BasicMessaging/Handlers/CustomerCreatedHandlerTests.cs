using MassTransit;
using MassTransit.Testing;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.BasicMessaging.Handlers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MassTransitDemo.Tests.BasicMessaging.Handlers;

public sealed class CustomerCreatedHandlerTests
{
    [Fact]
    public async Task Consume_CustomerCreatedEvent_LogsInformation()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<CustomerCreatedHandler>>();
        var publishEndpointMock = new Mock<IPublishEndpoint>();
        var handler = new CustomerCreatedHandler(loggerMock.Object, publishEndpointMock.Object);

        var message = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Email = "test@example.com"
        };

        var context = Mock.Of<ConsumeContext<CustomerCreated>>(c => c.Message == message);

        // Act
        await handler.Consume(context);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CustomerCreated event received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        publishEndpointMock.Verify(
            x => x.Publish(It.Is<SendVerificationEmail>(m => m.CustomerId == message.CustomerId), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
