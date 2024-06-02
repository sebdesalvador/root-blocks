namespace RootBlocks.Persistence.EntityFramework;

public abstract class DatabaseContextWithOutbox( DbContextOptions options )
    : DatabaseContext( options ), IDatabaseContextWithOutbox
{
    public DbSet< OutboxItem > Outbox { get; set; } = null!;

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        base.OnModelCreating( modelBuilder );
        modelBuilder.ApplyConfiguration( new OutboxItemConfiguration() );
    }
}
