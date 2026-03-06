using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.TopicFanout.Handlers;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.TopicFanout.Handlers;

public sealed class ShippingNotificationHandlerTests
{
    [Test]
    public async Task Consume_OrderShippedEvent_LogsInformation()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<ShippingNotificationHandler>>();
        var handler = new ShippingNotificationHandler(loggerFake);

        var message = new OrderShipped
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ShippedAt = DateTimeOffset.UtcNow,
            TrackingNumber = "TRACK-ABC123"
        };

        var context = A.Fake<ConsumeContext<OrderShipped>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await handler.Consume(context);

        // Assert
        A.CallTo(loggerFake)
            .Where(call => call.Method.Name == "Log" &&
                           call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                           call.GetArgument<object>(2)!.ToString()!.Contains(message.OrderId.ToString()))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Consume_OrderShippedEvent_DoesNotPublishOrSendFurtherMessages()
    {
        // Arrange — this handler is a terminal consumer; it must not produce new messages
        var loggerFake = A.Fake<ILogger<ShippingNotificationHandler>>();
        var handler = new ShippingNotificationHandler(loggerFake);

        var message = new OrderShipped
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ShippedAt = DateTimeOffset.UtcNow,
            TrackingNumber = "TRACK-XYZ789"
        };

        var context = A.Fake<ConsumeContext<OrderShipped>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await handler.Consume(context);

        // Assert: no publish or send calls on context
        A.CallTo(() => context.Publish(A<object>._, A<Type>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => context.GetSendEndpoint(A<Uri>._))
            .MustNotHaveHappened();
    }
}
