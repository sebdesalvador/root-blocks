namespace BuildingBlocks.Persistence.EntityFramework;

public abstract class DatabaseContext( DbContextOptions options, string defaultSchema = "dbo" )
    : DbContext( options ), IDatabaseContext
{
    public string DefaultSchema { get; } = defaultSchema;

    #region Overrides

    public override int SaveChanges()
    {
        UpdateDates();
        RemoveEnumerationsFromTracker();
        return base.SaveChanges();
    }

    public override async Task< int > SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        UpdateDates();
        RemoveEnumerationsFromTracker();
        return await base.SaveChangesAsync( acceptAllChangesOnSuccess, cancellationToken );
    }

    protected override void OnModelCreating( ModelBuilder modelBuilder )
    {
        modelBuilder.HasDefaultSchema( DefaultSchema );
        modelBuilder.ApplyDateTimeConverters();
        modelBuilder.AddDeletedOnToUniqueIndexes();
        base.OnModelCreating( modelBuilder );
    }

    #endregion

    #region Private Methods

    private void UpdateDates()
    {
        ChangeTracker.Entries()
                     .Where( ee => ee is { Entity: IDatable, State: EntityState.Added } )
                     .ToList()
                     .ForEach( ee =>
                      {
                          var utcNow = DateTime.UtcNow;
                          ( ( IDatable )ee.Entity ).CreatedOn = utcNow;
                          ( ( IDatable )ee.Entity ).LastModifiedOn = utcNow;
                      } );
        ChangeTracker.Entries()
                     .Where( ee => ee is { Entity: IDatable, State: EntityState.Modified } )
                     .ToList()
                     .ForEach( ee => ( ( IDatable )ee.Entity ).LastModifiedOn = DateTime.UtcNow );
        ChangeTracker.Entries()
                     .Where( ee => ee.State == EntityState.Deleted )
                     .ToList()
                     .ForEach( ee =>
                      {
                          var hasDeletedOnProperty = ee.Metadata
                                                       .FindProperty( nameof( ISoftDeletable.DeletedOn ) ) != null;

                          if ( !hasDeletedOnProperty )
                              return;

                          ee.State = EntityState.Modified;
                          ee.Property( nameof( ISoftDeletable.DeletedOn ) ).CurrentValue = DateTime.UtcNow;
                      } );
    }

    private void RemoveEnumerationsFromTracker()
    {
        ChangeTracker.Entries()
                     .Where( ee => ee.Entity is Enumeration )
                     .ToList()
                     .ForEach( ee => ee.State = EntityState.Detached );
    }

    #endregion
}
