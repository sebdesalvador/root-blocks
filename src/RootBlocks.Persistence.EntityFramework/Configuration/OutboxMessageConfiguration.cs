namespace RootBlocks.Persistence.EntityFramework.Configuration;

/// <summary>
/// Maps <see cref="OutboxMessage" /> to a table in the model's default schema, so that each context of a
/// modular monolith gets its own outbox. Applied by <see cref="Extensions.ModelBuilderExtensions.ApplyOutbox" />.
/// </summary>
public class OutboxMessageConfiguration( string tableName = OutboxMessageConfiguration.DEFAULT_TABLE_NAME )
    : IEntityTypeConfiguration< OutboxMessage >
{
    #region Constants

    public const string DEFAULT_TABLE_NAME = "Outbox";

    #endregion

    #region Interface Implementations

    public void Configure( EntityTypeBuilder< OutboxMessage > builder )
    {
        var dateTimeConverter = new ValueConverter< DateTime, DateTime >(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind( v, DateTimeKind.Utc ) );
        var nullableDateTimeConverter = new ValueConverter< DateTime?, DateTime? >(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind( v.Value, DateTimeKind.Utc ) : v );

        builder.ToTable( tableName );

        builder.HasKey( m => m.Id );

        builder.Property( m => m.Id )
               .ValueGeneratedNever();
        builder.Property( m => m.Type )
               .HasMaxLength( 512 )
               .IsRequired();
        builder.Property( m => m.Payload )
               .IsRequired();
        builder.Property( m => m.OccurredOn )
               .HasConversion( dateTimeConverter );
        builder.Property( m => m.ProcessedOn )
               .HasConversion( nullableDateTimeConverter );
        builder.Property( m => m.LastError );
        builder.Property( m => m.LockedUntil )
               .HasConversion( nullableDateTimeConverter );

        builder.HasIndex( m => new { m.ProcessedOn, m.OccurredOn } );
    }

    #endregion
}
