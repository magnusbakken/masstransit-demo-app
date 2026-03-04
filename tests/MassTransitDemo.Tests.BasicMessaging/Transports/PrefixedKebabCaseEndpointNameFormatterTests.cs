using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Transports;
using MassTransitDemo.Features.BasicMessaging.Handlers;

namespace MassTransitDemo.Tests.BasicMessaging.Transports;

public sealed class PrefixedKebabCaseEndpointNameFormatterTests
{
    [Test]
    public async Task Consumer_ReturnsKebabCaseNameWithPrefix()
    {
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("masstransitdemo.testuser");

        var name = formatter.Consumer<CustomerCreatedHandler>();

        await Assert.That(name).IsEqualTo("masstransitdemo.testuser.customer-created-handler");
    }

    [Test]
    public async Task Consumer_DelegatesToInnerFormatterAndPrefixes()
    {
        var inner = A.Fake<IEndpointNameFormatter>();
        A.CallTo(() => inner.Consumer<CustomerCreatedHandler>()).Returns("my-consumer");
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("demo.user", inner);

        var name = formatter.Consumer<CustomerCreatedHandler>();

        await Assert.That(name).IsEqualTo("demo.user.my-consumer");
    }

    [Test]
    public async Task Message_DoesNotApplyPrefix()
    {
        var inner = A.Fake<IEndpointNameFormatter>();
        A.CallTo(() => inner.Message<object>()).Returns("my-message");
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("demo.user", inner);

        var name = formatter.Message<object>();

        await Assert.That(name).IsEqualTo("my-message");
    }

    [Test]
    public async Task TemporaryEndpoint_AppliesPrefix()
    {
        var inner = A.Fake<IEndpointNameFormatter>();
        A.CallTo(() => inner.TemporaryEndpoint("tag")).Returns("temp-endpoint");
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("demo.user", inner);

        var name = formatter.TemporaryEndpoint("tag");

        await Assert.That(name).IsEqualTo("demo.user.temp-endpoint");
    }

    [Test]
    public async Task SanitizeName_DelegatesToInnerFormatter()
    {
        var inner = A.Fake<IEndpointNameFormatter>();
        A.CallTo(() => inner.SanitizeName("Foo Bar")).Returns("foo-bar");
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("demo.user", inner);

        var name = formatter.SanitizeName("Foo Bar");

        await Assert.That(name).IsEqualTo("foo-bar");
    }

    [Test]
    public async Task Separator_DelegatesToInnerFormatter()
    {
        var inner = A.Fake<IEndpointNameFormatter>();
        A.CallTo(() => inner.Separator).Returns("-");
        var formatter = new PrefixedKebabCaseEndpointNameFormatter("demo.user", inner);

        await Assert.That(formatter.Separator).IsEqualTo("-");
    }

    [Test]
    public Task Constructor_ThrowsOnNullPrefix()
    {
        Assert.Throws<ArgumentNullException>(
            () => _ = new PrefixedKebabCaseEndpointNameFormatter(null!));
        return Task.CompletedTask;
    }

    [Test]
    public Task Constructor_ThrowsOnEmptyPrefix()
    {
        Assert.Throws<ArgumentException>(
            () => _ = new PrefixedKebabCaseEndpointNameFormatter(""));
        return Task.CompletedTask;
    }

    [Test]
    public Task Constructor_ThrowsOnWhitespacePrefix()
    {
        Assert.Throws<ArgumentException>(
            () => _ = new PrefixedKebabCaseEndpointNameFormatter("   "));
        return Task.CompletedTask;
    }
}
