using MassTransitDemo.Features.Sagas.ConsumerSaga;
using MassTransitDemo.Features.Sagas.StateMachineSaga;
using Microsoft.EntityFrameworkCore;

namespace MassTransitDemo.Features.Sagas.Data;

/// <summary>
/// DbContext for Entity Framework–backed saga persistence.
/// Maps both the consumer saga and the state machine saga.
/// </summary>
public sealed class SagaDbContext : DbContext
{
    public SagaDbContext(DbContextOptions<SagaDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShipmentPreparationSaga> ShipmentPreparationSagas { get; set; } = null!;
    public DbSet<ShipmentPreparationState> ShipmentPreparationStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShipmentPreparationSaga>(entity =>
        {
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).ValueGeneratedNever();
            entity.Property(x => x.OrderConfirmed);
            entity.Property(x => x.InventoryReserved);
            entity.Property(x => x.CustomerId);
            entity.Property(x => x.ConfirmedAt);
            entity.Property(x => x.ReservedAt);
        });

        modelBuilder.Entity<ShipmentPreparationState>(entity =>
        {
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).ValueGeneratedNever();
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.OrderConfirmed);
            entity.Property(x => x.InventoryReserved);
            entity.Property(x => x.CustomerId);
            entity.Property(x => x.ConfirmedAt);
            entity.Property(x => x.ReservedAt);
        });
    }
}
