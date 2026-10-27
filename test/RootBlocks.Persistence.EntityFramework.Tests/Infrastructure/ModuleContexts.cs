namespace RootBlocks.Persistence.EntityFramework.Tests.Infrastructure;

/// <summary>
/// A module of a modular monolith: its own context, schema and outbox.
/// </summary>
public abstract class ModuleContext( DbContextOptions options, string schema ) : DatabaseContext( options, schema )
{
    public DbSet< Order > Orders { get; init; } = null!;

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.ApplyConfiguration( new OrderConfiguration() );
        modelBuilder.ApplyOutbox();
        base.OnModelCreating( modelBuilder );
    }
}

public class SalesContext( DbContextOptions< SalesContext > options ) : ModuleContext( options, "sales" );

public class BillingContext( DbContextOptions< BillingContext > options ) : ModuleContext( options, "billing" );

public class ModuleUnitOfWork< TContext >( TContext context, IEventPublisher eventPublisher, OutboxPublisher outboxPublisher )
    : UnitOfWork( context, eventPublisher, outboxPublisher )
    where TContext : DatabaseContext;
