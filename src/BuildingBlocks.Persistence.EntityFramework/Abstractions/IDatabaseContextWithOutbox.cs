namespace BuildingBlocks.Persistence.EntityFramework.Abstractions;

public interface IDatabaseContextWithOutbox : IDatabaseContext
{
    public DbSet< OutboxItem > Outbox { get; set; }
}
