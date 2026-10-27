namespace RootBlocks.Persistence.EntityFramework.Abstractions;

[ Obsolete( "Only used by AddOutboxListener. Map the outbox with modelBuilder.ApplyOutbox() and call AddOutbox< TContext >( configuration )." ) ]
public interface IDatabaseContextWithOutbox : IDatabaseContext
{
    public DbSet< OutboxMessage > Outbox { get; set; }
}
