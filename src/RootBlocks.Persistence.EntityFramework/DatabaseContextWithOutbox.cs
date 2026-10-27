namespace RootBlocks.Persistence.EntityFramework;

[ Obsolete( "Derive from DatabaseContext and call modelBuilder.ApplyOutbox() in OnModelCreating instead." ) ]
public abstract class DatabaseContextWithOutbox : DatabaseContext, IDatabaseContextWithOutbox
{
    public DbSet< OutboxMessage > Outbox { get; set; } = null!;

    protected DatabaseContextWithOutbox( DbContextOptions options ) : base( options ) { }

    protected DatabaseContextWithOutbox( DbContextOptions options, string defaultSchema ) : base( options, defaultSchema ) { }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.ApplyOutbox();
        base.OnModelCreating( modelBuilder );
    }
}
