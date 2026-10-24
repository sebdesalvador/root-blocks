namespace RootBlocks.Persistence.EntityFramework.Configuration;

/// <summary>
/// Base configuration for entities: table name, key, audit columns, concurrency token.
/// </summary>
/// <remarks>
/// The key is mapped as-is. When <typeparamref name="TId"/> is not a type EF maps natively, such as a
/// strongly-typed identifier, its value conversion must come from a convention registered in
/// <c>ConfigureConventions</c> (for instance StronglyTypedIdentifier's <c>UseStronglyTypedIdentifiers()</c>).
/// </remarks>
public abstract class EntityConfiguration< TEntity, TId > : IEntityTypeConfiguration< TEntity >
    where TEntity : Entity< TId >
    where TId : IEquatable< TId >
{
    public virtual void Configure( EntityTypeBuilder< TEntity > builder )
    {
        builder.ToTable( typeof( TEntity ).Name );

        builder.HasKey( e => e.Id );

        builder.Property( e => e.Id )
               .ValueGeneratedNever()
               .IsRequired();
        builder.Property( e => e.CreatedOn )
               .IsRequired();
        builder.Property( e => e.LastModifiedOn )
               .IsRequired();
        builder.Property< byte[] >( "RowVersion" )
               .IsConcurrencyToken()
               .ValueGeneratedOnAddOrUpdate()
               .IsRequired( false );
        builder.Ignore( e => e.DomainEvents );
    }
}
