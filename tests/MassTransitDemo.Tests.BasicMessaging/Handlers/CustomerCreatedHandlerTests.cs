using FakeItEasy;
using MassTransit;
using MassTransit.Testing;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.BasicMessaging.Handlers;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.BasicMessaging.Handlers;

public sealed class CustomerCreatedHandlerTests
{
    [Test]
    public async Task Consume_CustomerCreatedEvent_LogsInformation()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<CustomerCreatedHandler>>();
        var publishEndpointFake = A.Fake<IPublishEndpoint>();
        var handler = new CustomerCreatedHandler(loggerFake, publishEndpointFake);

        var message = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Email = "test@example.com"
        };

        var context = A.Fake<ConsumeContext<CustomerCreated>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await handler.Consume(context);

        // Assert
        A.CallTo(() => loggerFake.Log(
                LogLevel.Information,
                A<EventId>._,
                A<object>._,
                A<Exception?>._,
                A<Func<object, Exception?, string>>._))
            .WhenArgumentsMatch((LogLevel level, EventId eventId, object state, Exception? ex, Func<object, Exception?, string> formatter) =>
                state.ToString()!.Contains("CustomerCreated event received"))
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => publishEndpointFake.Publish(
                A<SendVerificationEmail>.That.Matches(m => m.CustomerId == message.CustomerId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }
}
