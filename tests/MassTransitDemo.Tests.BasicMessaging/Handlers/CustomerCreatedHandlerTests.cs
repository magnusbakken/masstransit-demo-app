using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Core.Transports;
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
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("test");
        var handler = new CustomerCreatedHandler(loggerFake, formatter);

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
        A.CallTo(loggerFake)
            .Where(call => call.Method.Name == "Log" &&
                           call.GetArgument<LogLevel>(0) == LogLevel.Information &&
                           call.GetArgument<object>(2)!.ToString()!.Contains("CustomerCreated event received"))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Consume_CustomerCreatedEvent_SendsVerificationEmailCommand()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<CustomerCreatedHandler>>();
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("test");
        var handler = new CustomerCreatedHandler(loggerFake, formatter);

        var message = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Email = "test@example.com"
        };

        var sendEndpointFake = A.Fake<ISendEndpoint>();
        var context = A.Fake<ConsumeContext<CustomerCreated>>();
        A.CallTo(() => context.Message).Returns(message);
        A.CallTo(() => context.GetSendEndpoint(A<Uri>._))
            .Returns(Task.FromResult(sendEndpointFake));

        // Act
        await handler.Consume(context);

        // Assert: command is sent (not published) to the SendVerificationEmailHandler queue
        A.CallTo(() => sendEndpointFake.Send(
                A<SendVerificationEmail>.That.Matches(m => m.CustomerId == message.CustomerId),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Consume_CustomerCreatedEvent_SendsToCorrectEndpoint()
    {
        // Arrange
        var loggerFake = A.Fake<ILogger<CustomerCreatedHandler>>();
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("test");
        var handler = new CustomerCreatedHandler(loggerFake, formatter);

        var message = new CustomerCreated
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            Email = "test@example.com"
        };

        Uri? capturedAddress = null;
        var sendEndpointFake = A.Fake<ISendEndpoint>();
        var context = A.Fake<ConsumeContext<CustomerCreated>>();
        A.CallTo(() => context.Message).Returns(message);
        A.CallTo(() => context.GetSendEndpoint(A<Uri>._))
            .Invokes(call => capturedAddress = call.GetArgument<Uri>(0))
            .Returns(Task.FromResult(sendEndpointFake));

        // Act
        await handler.Consume(context);

        // Assert: the queue address matches the SendVerificationEmailHandler consumer endpoint
        var expectedQueue = formatter.Consumer<SendVerificationEmailHandler>();
        await Assert.That(capturedAddress).IsNotNull();
        await Assert.That(capturedAddress!.ToString()).Contains(expectedQueue);
    }
}
