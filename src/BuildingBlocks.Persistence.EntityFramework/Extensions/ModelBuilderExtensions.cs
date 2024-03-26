namespace BuildingBlocks.Persistence.EntityFramework.Extensions;

public static class ModelBuilderExtensions
{
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
