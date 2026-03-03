using FakeItEasy;
using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Sagas.ConsumerSaga;
using Microsoft.Extensions.Logging;

namespace MassTransitDemo.Tests.Sagas.Sagas;

public sealed class ShipmentPreparationSagaTests
{
    [Test]
    public async Task Consume_OrderConfirmed_UpdatesSagaState()
    {
        // Arrange
        var saga = new ShipmentPreparationSaga();
        var message = new OrderConfirmed
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ConfirmedAt = DateTime.UtcNow
        };

        var context = A.Fake<ConsumeContext<OrderConfirmed>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await saga.Consume(context);

        // Assert
        await Assert.That(saga.CorrelationId).IsEqualTo(message.OrderId);
        await Assert.That(saga.OrderConfirmed).IsTrue();
        await Assert.That(saga.CustomerId).IsEqualTo(message.CustomerId);
        await Assert.That(saga.ConfirmedAt).IsEqualTo(message.ConfirmedAt);
    }

    [Test]
    public async Task Consume_InventoryReserved_UpdatesSagaState()
    {
        // Arrange
        var saga = new ShipmentPreparationSaga();
        var message = new InventoryReserved
        {
            OrderId = Guid.NewGuid(),
            Items = new List<ReservedItem>
            {
                new ReservedItem { ProductId = "PROD-001", Quantity = 2 }
            },
            ReservedAt = DateTime.UtcNow
        };

        var context = A.Fake<ConsumeContext<InventoryReserved>>();
        A.CallTo(() => context.Message).Returns(message);

        // Act
        await saga.Consume(context);

        // Assert
        await Assert.That(saga.CorrelationId).IsEqualTo(message.OrderId);
        await Assert.That(saga.InventoryReserved).IsTrue();
        await Assert.That(saga.ReservedAt).IsEqualTo(message.ReservedAt);
    }
}
