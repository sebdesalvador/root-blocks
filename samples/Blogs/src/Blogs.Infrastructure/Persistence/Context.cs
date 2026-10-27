namespace Blogs.Infrastructure.Persistence;

[ ExcludeFromCodeCoverage ]
public sealed class Context( DbContextOptions options ) : DatabaseContext( options )
{
    public DbSet< Person > People { get; init; } = null!;
    public DbSet< Blog > Blogs { get; init; } = null!;
    public DbSet< Post > Posts { get; init; } = null!;

    protected override void ConfigureConventions( ModelConfigurationBuilder configurationBuilder )
    {
        configurationBuilder.UseStronglyTypedIdentifiers( typeof( BlogId ).Assembly );
        base.ConfigureConventions( configurationBuilder );
    }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.ApplyConfigurationsFromAssembly( Assembly.GetExecutingAssembly() );
        modelBuilder.ApplyOutbox();
        base.OnModelCreating( modelBuilder );
    }
}
