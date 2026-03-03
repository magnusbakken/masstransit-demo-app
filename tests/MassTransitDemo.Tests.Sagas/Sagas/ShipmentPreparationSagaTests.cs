using MassTransit;
using MassTransitDemo.Core.Messages;
using MassTransitDemo.Features.Sagas.ConsumerSaga;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MassTransitDemo.Tests.Sagas.Sagas;

public sealed class ShipmentPreparationSagaTests
{
    [Fact]
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

        var context = Mock.Of<ConsumeContext<OrderConfirmed>>(c => c.Message == message);

        // Act
        await saga.Consume(context);

        // Assert
        Assert.Equal(message.OrderId, saga.CorrelationId);
        Assert.True(saga.OrderConfirmed);
        Assert.Equal(message.CustomerId, saga.CustomerId);
        Assert.Equal(message.ConfirmedAt, saga.ConfirmedAt);
    }

    [Fact]
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

        var context = Mock.Of<ConsumeContext<InventoryReserved>>(c => c.Message == message);

        // Act
        await saga.Consume(context);

        // Assert
        Assert.Equal(message.OrderId, saga.CorrelationId);
        Assert.True(saga.InventoryReserved);
        Assert.Equal(message.ReservedAt, saga.ReservedAt);
    }
}
