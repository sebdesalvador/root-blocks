namespace BuildingBlocks.Persistence.EntityFramework.Configuration;

public abstract class EntityConfiguration< TEntity, TIdentity > : IEntityTypeConfiguration< TEntity >
    where TEntity : Entity< TIdentity >
    where TIdentity : Identity, new()
{
    public virtual void Configure( EntityTypeBuilder< TEntity > builder )
    {
        builder.ToTable( typeof( TEntity ).Name );

        builder.HasKey( e => e.Id );

        builder.Property( e => e.Id )
               .ValueGeneratedNever()
               .HasConversion( new IdentityToGuidConverter< TIdentity >() )
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
