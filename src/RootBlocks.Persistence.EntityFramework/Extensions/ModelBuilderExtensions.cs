namespace RootBlocks.Persistence.EntityFramework.Extensions;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds the transactional outbox to the model: an <see cref="OutboxMessage" /> table in the model's default
    /// schema (set it with <c>HasDefaultSchema</c>). Any <see cref="DbContext" /> can call it, so each context of
    /// a modular monolith gets its own outbox table in its own schema.
    /// </summary>
    public static ModelBuilder ApplyOutbox(
        this ModelBuilder builder,
        string tableName = OutboxMessageConfiguration.DEFAULT_TABLE_NAME
    ) => builder.ApplyConfiguration( new OutboxMessageConfiguration( tableName ) );

    public static void AddDeletedOnToUniqueIndexes( this ModelBuilder builder )
    {
        foreach ( var entityType in builder.Model.GetEntityTypes() )
        {
            var hasDeletedOnProperty = entityType.FindProperty( nameof( ISoftDeletable.DeletedOn ) ) != null;

            if ( !hasDeletedOnProperty )
                continue;

            foreach ( var index in entityType.GetIndexes().ToList() )
            {
                if ( !index.IsUnique )
                    continue;

                var indexPropertiesNames = index.Properties.Select( p => p.Name ).ToList();

                if ( indexPropertiesNames.Contains( nameof( ISoftDeletable.DeletedOn ) ) )
                    continue;

                indexPropertiesNames.Add( nameof( ISoftDeletable.DeletedOn ) );
                entityType.RemoveIndex( index.Properties );
                var newIndex = entityType.AddIndex( entityType.FindProperties( indexPropertiesNames )
                                                 ?? throw new InvalidOperationException() );
                newIndex.SetFilter( $"[{nameof( ISoftDeletable.DeletedOn )}] IS NULL" );
                newIndex.IsUnique = true;
            }
        }
    }

    public static void ApplyDateTimeConverters( this ModelBuilder builder )
    {
        var dateTimeConverter = new ValueConverter< DateTime, DateTime >(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind( v, DateTimeKind.Utc ) );
        var nullableDateTimeConverter = new ValueConverter< DateTime?, DateTime? >(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind( v.Value, DateTimeKind.Utc ) : v );

        foreach ( var entityType in builder.Model.GetEntityTypes() )
            foreach ( var property in entityType.GetProperties() )
                if ( property.ClrType == typeof( DateTime ) )
                    property.SetValueConverter( dateTimeConverter );
                else if ( property.ClrType == typeof( DateTime? ) )
                    property.SetValueConverter( nullableDateTimeConverter );
    }
}
